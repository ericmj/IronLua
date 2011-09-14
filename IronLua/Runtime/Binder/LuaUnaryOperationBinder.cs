using System;
using System.Dynamic;
using System.Linq.Expressions;
using Expr = System.Linq.Expressions.Expression;
using ExprType = System.Linq.Expressions.ExpressionType;
using IronLua.Util;

namespace IronLua.Runtime.Binder
{
    class LuaUnaryOperationBinder : UnaryOperationBinder
    {
        Context context;

        public LuaUnaryOperationBinder(Context context, ExprType operation)
            : base(operation)
        {
            this.context = context;
        }

        public override DynamicMetaObject FallbackUnaryOperation(DynamicMetaObject target, DynamicMetaObject errorSuggestion)
        {
            if (!target.HasValue)
                return Defer(target);

            DynamicMetaObject firstVarargs;
            if (RuntimeHelpers.TryGetFirstVarargs(target, out firstVarargs))
                return FallbackUnaryOperation(firstVarargs, errorSuggestion);

            Expr expression;
            switch (Operation)
            {
                case ExprType.Negate:
                    if (target.LimitType == typeof(double))
                        expression = Expr.MakeUnary(Operation, Expr.Convert(target.Expression, typeof(double)), null);
                    else
                        throw new Exception(); // TODO: Use errorSuggestion
                    break;
                case ExprType.Not:
                    if (target.LimitType == typeof(bool))
                        expression = Expr.MakeUnary(Operation, target.Expression, null);
                    else
                        expression = Expr.Equal(target.Expression, Expr.Constant(null));
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            return new DynamicMetaObject(Expr.Convert(expression, typeof(object)), RuntimeHelpers.MergeTypeRestrictions(target));
        }
    }
}