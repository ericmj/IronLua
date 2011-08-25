using System.Dynamic;
using System.Linq;
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
            // TODO: Implement LuaFunction that stores metadata on parameters so passing tables to use named parameters
            //       and optional parameters work

            if (!target.HasValue || args.Any(a => !a.HasValue))
                return Defer(target, args);

            var restrictions = target.Restrictions.Merge(
                BindingRestrictions.GetTypeRestriction(target.Expression, target.LimitType));

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