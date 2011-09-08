using System;
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
            // TODO: Optional parameters and passing table for named parameters

            // NOTE: We may only need a type restriction on the first argument since we only need special
            //       handling for LuaTable when it's the lone argument. Although we may need instance restrict
            //       on that argument then, or probably instance restrict on the function instead, since we
            //       only want to restrict parameter names not argument values also
            var restrictions = target.MergeTypeRestrictions(args);

            var expression =
                Expr.Convert(
                    Expr.Invoke(
                        Expr.Convert(target.Expression, target.LimitType),
                        args.Select(a => a.Expression)),
                    typeof(object));

            return new DynamicMetaObject(expression, restrictions);
        }
    }
}