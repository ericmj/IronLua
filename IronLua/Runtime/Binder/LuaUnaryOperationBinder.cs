using System.Dynamic;
using System.Linq.Expressions;
using Expr = System.Linq.Expressions.Expression;
using ExprType = System.Linq.Expressions.ExpressionType;

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

            switch (Operation)
            {
                case ExprType.Negate:
                    break;
                case ExprType.Not:
                    break;
            }

            return null;
        }
    }
}