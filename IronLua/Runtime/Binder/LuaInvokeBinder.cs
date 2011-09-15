using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using IronLua.Util;
using Expr = System.Linq.Expressions.Expression;

namespace IronLua.Runtime.Binder
{
    class LuaInvokeBinder : InvokeBinder
    {
        public LuaInvokeBinder(CallInfo callInfo)
            : base(callInfo)
        {
        }

        public override DynamicMetaObject FallbackInvoke(DynamicMetaObject target, DynamicMetaObject[] args, DynamicMetaObject errorSuggestion)
        {
            if (!target.HasValue || args.Any(a => !a.HasValue))
                return Defer(target, args);

            var restrictions =
                RuntimeHelpers.MergeTypeRestrictions(target, args).Merge(
                RuntimeHelpers.MergeInstanceRestrictions(target));

            var function = (Delegate)target.Value; // TODO: Check type cast
            var mappedArgs = MapArguments(args, function.Method.GetParameters(), ref restrictions);

            var expression =
                Expr.Convert(
                    Expr.Invoke(
                        Expr.Convert(target.Expression, target.LimitType),
                        mappedArgs),
                    typeof(object));

            return new DynamicMetaObject(expression, restrictions);
        }

        IEnumerable<Expr> MapArguments(DynamicMetaObject[] args, ParameterInfo[] parameterInfos, ref BindingRestrictions restrictions)
        {
            var arguments = args.Select(arg => new Argument(arg.Expression, arg.LimitType)).ToList();

            // Remove closure
            ParameterInfo[] parameters;
            if (parameterInfos[0].ParameterType == typeof(Closure))
            {
                parameters = new ParameterInfo[parameterInfos.Length - 1];
                Array.Copy(parameterInfos, 1, parameters, 0, parameters.Length);
            }
            else
            {
                parameters = parameterInfos;
            }

            ExpandLastArg(arguments, args, ref restrictions);
            DefaultParamValues(arguments, parameters);
            DefaultTypeValues(arguments, parameters);
            OverflowIntoParams(arguments, parameters);
            // TODO: Type coercion
            CastToParamType(arguments, parameters);

            return arguments.Select(arg => arg.Expression);
        }

        void ExpandLastArg(List<Argument> arguments, DynamicMetaObject[] args, ref BindingRestrictions restrictions)
        {
            if (args.Length == 0)
                return;

            var lastArg = args.Last();

            if (lastArg.LimitType != typeof(Varargs))
                return;

            restrictions = restrictions.Merge(RuntimeHelpers.MergeInstanceRestrictions(lastArg));

            var varargs = (Varargs)lastArg.Value;
            arguments.AddRange(varargs.Select(value => new Argument(Expr.Constant(value), value.GetType())));
        }

        void DefaultParamValues(List<Argument> arguments, ParameterInfo[] parameters)
        {
            var defaultArgs = parameters
                .Skip(arguments.Count)
                .Where(param => param.IsOptional)
                .Select(param => new Argument(Expr.Constant(param.DefaultValue), param.ParameterType));

            arguments.AddRange(defaultArgs);
        }

        void DefaultTypeValues(List<Argument> arguments, ParameterInfo[] parameters)
        {
            var typeDefaultArgs = parameters
                .Skip(arguments.Count)
                .Select(param => new Argument(Expr.Constant(param.ParameterType.GetDefaultValue()), param.ParameterType));
            arguments.AddRange(typeDefaultArgs);
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
                    throw new Exception(); // TODO

                argExpr = Expr.NewArrayInit(
                    elementType,
                    overflowingArgs.Select(arg => Expr.Convert(arg.Expression, elementType)));
            }
            else if (lastParam.ParameterType == typeof(Varargs))
            {
                argExpr = Expr.New(
                    typeof(Varargs).GetConstructor(new[] {typeof(object[])}),
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

        void CastToParamType(List<Argument> arguments, ParameterInfo[] parameters)
        {
            for (int i = 0; i < arguments.Count; i++)
            {
                // NOTE: Do we need to check if we can do this cast? Sympl doesn't.
                arguments[i].Expression = Expr.Convert(arguments[i].Expression, parameters[i].ParameterType);
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