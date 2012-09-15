using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using IronLua.Compiler;
using IronLua.Library;
using IronLua.Util;
using Microsoft.Scripting.Utils;
using Expr = System.Linq.Expressions.Expression;
using Microsoft.Scripting.Actions;

namespace IronLua.Runtime.Binder
{
    class LuaInvokeBinder : InvokeBinder
    {
        readonly LuaContext context;

        public LuaInvokeBinder(LuaContext context, CallInfo callInfo)
            : base(callInfo)
        {
            ContractUtils.RequiresNotNull(context, "context");
            this.context = context;
        }

        public override DynamicMetaObject FallbackInvoke(DynamicMetaObject target, DynamicMetaObject[] args, DynamicMetaObject errorSuggestion)
        {
            if (!target.HasValue || args.Any(a => !a.HasValue))
                return Defer(target, args);

            var restrictions = RuntimeHelpers.MergeTypeRestrictions(target);

            if (target.Value == null)
                throw new LuaRuntimeException(context, "Attempt to invoke a nil object");

            if (!target.LimitType.IsSubclassOf(typeof(Delegate)) && target.LimitType != typeof(Varargs))
                return new DynamicMetaObject(MetamethodFallbacks.Call(context, target, args), restrictions);
            


            restrictions = restrictions.Merge(
                RuntimeHelpers.MergeTypeRestrictions(args).Merge(
                    RuntimeHelpers.MergeInstanceRestrictions(target)));

            Delegate function = null;
            DynamicMetaObject actualTarget = null;
            if (target.LimitType.IsSubclassOf(typeof(Delegate)))
            {
                function = (Delegate)target.Value;
                actualTarget = target;
            }
            else if (target.LimitType == typeof(Varargs))
            {
                function = ((Varargs)target.Value).First() as Delegate;
                actualTarget = new DynamicMetaObject(Expression.Constant(((Varargs)target.Value).First()), BindingRestrictions.Empty, ((Varargs)target.Value).First());
            }

            if (function == null)
                return new DynamicMetaObject(MetamethodFallbacks.Call(context,
                    actualTarget, 
                    args),
                    restrictions);

            var methodInfo = function.Method;

            bool toss = false;
            return GetInvoker(actualTarget, args, methodInfo, out toss, ref restrictions);
        }

        private DynamicMetaObject GetInvoker(DynamicMetaObject target, DynamicMetaObject[] args, MethodInfo methodInfo, out bool success, ref BindingRestrictions restrictions)
        {
            Expr failExpr;
            List<Expr> sideEffects;

            var mappedArgs = MapArguments(args, methodInfo, ref restrictions, out sideEffects, out failExpr);

            success = failExpr == null;

            if (!success)
                return new DynamicMetaObject(Expr.Block(failExpr, Expr.Default(typeof(object))), restrictions);


            var invokeExpr = InvokeExpression(target, mappedArgs, methodInfo);

            // Execute overflowing arguments for side effects
            Expr expr;
            if (sideEffects.Count == 0)
            {
                expr = invokeExpr;
            }
            else
            {
                var tempVar = Expr.Variable(typeof(object));
                var assign = Expr.Assign(tempVar, invokeExpr);
                sideEffects.Insert(0, assign);
                sideEffects.Add(tempVar);
                expr = Expr.Block(new[] { tempVar }, sideEffects);
            }

            return new DynamicMetaObject(expr, restrictions);
        }

        static Expr InvokeExpression(DynamicMetaObject target, IEnumerable<Expr> mappedArgs, MethodInfo methodInfo)
        {
            var invokeExpr = Expr.Invoke(
                Expr.Convert(target.Expression, target.LimitType),
                mappedArgs);

            if (methodInfo.ReturnType == typeof(void))
                return Expr.Block(invokeExpr, Expr.Default(typeof(object)));
            return Expr.Convert(invokeExpr, typeof(object));
        }

        IEnumerable<Expr> MapArguments(DynamicMetaObject[] args, MethodInfo methodInfo, ref BindingRestrictions restrictions, out List<Expr> sideEffects, out Expr failExpr)
        {
            var parameters = methodInfo.GetParameters();
            var isFromLua = methodInfo.Name.StartsWith(Constant.FUNCTION_PREFIX);
            var arguments = args.Select(arg => new Argument(arg.Expression, arg.LimitType)).ToList();

            // Remove closure
            if (parameters.Length > 0 && parameters[0].ParameterType == typeof(Closure))
            {
                var tempParameters = new ParameterInfo[parameters.Length - 1];
                Array.Copy(parameters, 1, tempParameters, 0, tempParameters.Length);
                parameters = tempParameters;
            }

            ExpandLastArg(arguments, args, ref restrictions);
            DefaultParamValues(arguments, parameters);
            OverflowIntoParams(arguments, parameters);
            TrimArguments(arguments, parameters, out sideEffects);
            if (isFromLua)
                DefaultParamTypeValues(arguments, parameters);
            ConvertArgumentToParamType(arguments, parameters, out failExpr);
            if (failExpr == null && !isFromLua)
                CheckNumberOfArguments(arguments, parameters, out failExpr);

            return arguments.Select(arg => arg.Expression);
        }

        void ExpandLastArg(List<Argument> arguments, DynamicMetaObject[] args, ref BindingRestrictions restrictions)
        {
            if (args.Length == 0)
                return;

            var lastArg = args.Last();

            if (lastArg.LimitType != typeof(Varargs))
                return;

            // TODO: Use custom restriction (checks length and types) and add arguments by indexing into Varargs value
            restrictions = restrictions.Merge(RuntimeHelpers.MergeInstanceRestrictions(lastArg));

            var varargs = (Varargs)lastArg.Value;
            arguments.RemoveAt(arguments.Count - 1);
            arguments.AddRange(varargs.Select(value => new Argument(Expr.Constant(value), (value ?? new object()).GetType())));
        }

        void DefaultParamValues(List<Argument> arguments, ParameterInfo[] parameters)
        {
            var defaultArgs = parameters
                .Skip(arguments.Count)
                .Where(param => param.IsOptional)
                .Select(param => new Argument(Expr.Constant(param.DefaultValue), param.ParameterType));

            arguments.AddRange(defaultArgs);
        }

        void OverflowIntoParams(List<Argument> arguments, ParameterInfo[] parameters)
        {
            if (arguments.Count == 0 || parameters.Length == 0)
                return;

            var overflowingArgs = arguments.Skip(parameters.Length - 1).ToList();
            var lastParam = parameters.Last();

            if (overflowingArgs.Count == 1 && overflowingArgs[0].Type == lastParam.ParameterType)
                return;

            Expr argExpr;
            if (lastParam.IsParams())
            {
                var elementType = lastParam.ParameterType.GetElementType();
                if (overflowingArgs.Any(arg => arg.Type != elementType && !arg.Type.IsSubclassOf(elementType)))
                    return;

                argExpr = Expr.NewArrayInit(
                    elementType,
                    overflowingArgs.Select(arg => Expr.Convert(arg.Expression, elementType)));
            }
            else if (lastParam.ParameterType == typeof(Varargs))
            {
                argExpr = Expr.New(
                    MemberInfos.NewVarargs,
                    Expr.NewArrayInit(
                        typeof(object),
                        overflowingArgs.Select(arg => Expr.Convert(arg.Expression, typeof(object)))));
            }
            else
            {
                return;
            }

            arguments.RemoveRange(arguments.Count - overflowingArgs.Count, overflowingArgs.Count);
            arguments.Add(new Argument(argExpr, lastParam.ParameterType));
        }

        void TrimArguments(List<Argument> arguments, ParameterInfo[] parameters, out List<Expr> sideEffects)
        {
            if (arguments.Count <= parameters.Length)
            {
                sideEffects = new List<Expr>();
                return;
            }

            sideEffects = arguments
                .Skip(parameters.Length)
                .Select(arg => arg.Expression)
                .ToList();
            arguments.RemoveRange(parameters.Length, arguments.Count - parameters.Length);
        }

        void DefaultParamTypeValues(List<Argument> arguments, ParameterInfo[] parameters)
        {
            var typeDefaultArgs = parameters
                .Skip(arguments.Count)
                .Select(param => new Argument(Expr.Constant(param.ParameterType.GetDefaultValue()), param.ParameterType));
            arguments.AddRange(typeDefaultArgs);
        }

        void ConvertArgumentToParamType(List<Argument> arguments, ParameterInfo[] parameters, out Expr failExpr)
        {
            failExpr = null;

            for (int i = 0; i < arguments.Count; i++)
            {
                var arg = arguments[i];
                var param = parameters[i];

                if (param.ParameterType == typeof(bool) && arg.Type != typeof(bool))
                {
                    arg.Expression = ExprHelpers.ConvertToBoolean(context, arg.Expression);
                }
                else if (param.ParameterType == typeof(double) && arg.Type == typeof(string))
                {
                    arg.Expression = ExprHelpers.ConvertToNumberAndCheck(
                        context, arg.Expression,
                        ExceptionMessage.INVOKE_BAD_ARGUMENT_GOT, i + 1, "number", "string");
                }
                else if (param.ParameterType == typeof(string) && arg.Type != typeof(string))
                {
                    arg.Expression = Expr.Call(arg.Expression, MemberInfos.ObjectToString);
                }
                else
                {
                    if (arg.Type == param.ParameterType || arg.Type.IsSubclassOf(param.ParameterType))
                    {
                        arg.Expression = Expr.Convert(arg.Expression, param.ParameterType);
                    }
                    else
                    {
                        Func<Expr, Expr> typeNameExpr =
                            obj => Expr.Invoke(
                                Expr.Constant((Func<object, string>)BaseLibrary.Type),
                                Expr.Convert(obj, typeof(object)));

                        // Ugly reflection hack
                        failExpr = Expr.Throw(
                            Expr.New(
                                MemberInfos.NewRuntimeException,
                                Expr.Constant(ExceptionMessage.INVOKE_BAD_ARGUMENT_GOT),
                                Expr.NewArrayInit(
                                    typeof(object),
                                    Expr.Constant(i + 1, typeof(object)),
                                    typeNameExpr(Expr.Constant(Activator.CreateInstance(param.ParameterType))),
                                    typeNameExpr(arg.Expression))));
                        break;
                    }
                }
            }
        }

        void CheckNumberOfArguments(List<Argument> arguments, ParameterInfo[] parameters, out Expr failExpr)
        {
            failExpr = null;
            Debug.Assert(arguments.Count <= parameters.Length);

            if (arguments.Count < parameters.Length)
            {
                failExpr = Expr.Throw(
                    Expr.New(
                        MemberInfos.NewRuntimeException,
                        Expr.Constant(ExceptionMessage.INVOKE_BAD_ARGUMENT_EXPECTED),
                        Expr.Constant(new object[] {arguments.Count + 1, "value"})));
            }
        }

        class Argument
        {
            public Expr Expression { get; set; }
            public Type Type { get; private set; }

            public Argument(Expr expression, Type type)
            {
                Expression = expression;
                Type = type;
            }
        }
    }
}