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

            Expr expression = null;
            switch (Operation)
            {
                case ExprType.Negate:
                    expression = Expr.MakeUnary(Operation, target.Expression, null);
                    break;
                case ExprType.Not:
                    if (target.LimitType == typeof(bool))
                        expression = Expr.MakeUnary(Operation, target.Expression, null);
                    else
                        expression = Expr.MakeBinary(ExprType.Equal, target.Expression, Expr.Constant(null));
                    break;
            }

            return new DynamicMetaObject(Expr.Convert(expression, typeof(object)), target.MergeTypeRestrictions());
        }
    }
}