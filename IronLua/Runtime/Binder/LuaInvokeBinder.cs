using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Reflection;
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
            // TODO: Handle passing table as single argument for named parameters
            
            if (!target.HasValue || args.Any(a => !a.HasValue))
                return Defer(target, args);

            var restrictions = target
                .MergeTypeRestrictions(args)
                .Merge(BindingRestrictions.GetInstanceRestriction(target.Expression, target.Value));

            var resizeArgs = OverloadArgs(target, args, restrictions);

            var expression =
                Expr.Convert(
                    Expr.Invoke(
                        Expr.Convert(target.Expression, target.LimitType),
                        resizeArgs),
                    typeof(object));

            return new DynamicMetaObject(expression, restrictions);
        }

        // Pads the argument list with default values if there are less arguments than parameters to the function
        // and removes arguments if there are more arguments than parameters.
        IEnumerable<Expr> OverloadArgs(DynamicMetaObject target, DynamicMetaObject[] args, BindingRestrictions restrictions)
        {
            // TODO: Make sure that all args are evaluated, even if args.Length > maxArgs
            //       check this for LuaFunction also
            // TODO: Smart casting of arguments to match parameter type, do value coercion, cast to object if needed etc.
            var function = (Delegate)target.Value;
            var parameters = function.Method.GetParameters();
            var minArgs = parameters.Count(arg => !arg.IsOptional);
            var maxArgs = parameters.Length;


            if (args.Length == 0)
                return new Expr[] { };

            var argExprs = LuaInvokeBinder.ExpandVarargs(args, restrictions).ToList();

            if (argExprs.Count < minArgs)
                return null; // TODO

            var defaultValues = parameters
                .Skip(argExprs.Count)
                .Select(param => Expr.Constant(param.DefaultValue));

            // TODO: Check that the types of final args matches the target argument types
            return argExprs.Resize(maxArgs, defaultValues);
        }

        public static IEnumerable<Expr> ExpandVarargs(DynamicMetaObject[] args, BindingRestrictions restrictions)
        {
            var argExprs = args
                .Take(args.Length - 1)
                .Select(TryWrapWithVarargsSelect);

            var lastArg = args.Last();
            if (lastArg.LimitType == typeof(Varargs))
            {
                var varargs = (Varargs)lastArg.Value;
                argExprs = argExprs.Concat(ExpandVarargsIntoExprs(varargs));
                BindingRestrictions.GetInstanceRestriction(lastArg.Expression, lastArg.Value).Merge(restrictions);
            }
            else
            {
                argExprs = argExprs.Add(Expr.Convert(lastArg.Expression, typeof(object)));
            }

            return argExprs;
        }

        public static IEnumerable<Expr> ExpandVarargsIntoExprs(Varargs varargs)
        {
            var exprs = varargs.Take(varargs.Count - 1).Select(Expr.Constant);
            var lastArg = varargs.Last();

            if (lastArg.GetType() == typeof(Varargs))
                return exprs.Concat(ExpandVarargsIntoExprs((Varargs)lastArg));
            return exprs.Add(Expr.Constant(lastArg));
        }

        public static Expr TryWrapWithVarargsSelect(DynamicMetaObject dmo)
        {
            Expr expr;
            if (dmo.LimitType == typeof(Varargs))
                expr = Expr.Call(dmo.Expression, typeof(Varargs).GetMethod("First"));
            else
                expr = dmo.Expression;

            return Expr.Convert(expr, typeof(object));
        }
    }
}