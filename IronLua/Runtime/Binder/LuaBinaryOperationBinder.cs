using System;
using System.Dynamic;
using System.Linq.Expressions;

namespace IronLua.Runtime.Binder
{
    class LuaBinaryOperationBinder : BinaryOperationBinder
    {
        public LuaBinaryOperationBinder(ExpressionType op)
            : base(op)
        {
        }

        public override DynamicMetaObject FallbackBinaryOperation(DynamicMetaObject target, DynamicMetaObject arg, DynamicMetaObject errorSuggestion)
        {
            if (!target.HasValue || !arg.HasValue)
                return Defer(target, arg);

            var restrictions =
                target.Restrictions
                    .Merge(arg.Restrictions)
                    .Merge(BindingRestrictions.GetTypeRestriction(target.Expression, target.LimitType))
                    .Merge(BindingRestrictions.GetTypeRestriction(arg.Expression, arg.LimitType));

            throw new NotImplementedException();
        }
    }
}