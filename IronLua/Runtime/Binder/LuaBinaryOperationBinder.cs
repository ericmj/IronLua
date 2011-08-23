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

            var left = ConvertOperand(target);
            var right = ConvertOperand(arg);
            var result = Expression.Convert(Expression.MakeBinary(Operation, left, right), typeof(object));

            return new DynamicMetaObject(result, restrictions);
        }

        static Expression ConvertOperand(DynamicMetaObject metaObject)
        {
            // TODO: Parse string to double and look for metatable
            Expression op = null;
            if (metaObject.LimitType == typeof (double))
                op = metaObject.Expression;
            return op;
        }
    }
}