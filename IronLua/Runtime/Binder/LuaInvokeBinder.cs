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

            var resizeArgs = OverloadArgs(target, args);

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
        IEnumerable<Expr> OverloadArgs(DynamicMetaObject target, DynamicMetaObject[] args)
        {
            // TODO: Make sure that all args are evaluated, even if args.Length > maxArgs
            //       check this for LuaFunction also
            // TODO: Smart casting of arguments to match parameter type, do value coercion, cast to object if needed etc.
            var function = (Delegate)target.Value;
            var parameters = function.Method.GetParameters();
            var minArgs = parameters.Count(arg => !arg.IsOptional);
            var maxArgs = parameters.Length;

            if (args.Length < minArgs)
                return null; // TODO

            var defaultValues = parameters
                .Skip(args.Length)
                .Select(param => Expr.Constant(param.DefaultValue));

            return args.Select(a => a.Expression).Resize(maxArgs, defaultValues);
        }
    }
}