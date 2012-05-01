using System;
using System.Dynamic;
using Microsoft.Scripting.Utils;
using Expr = System.Linq.Expressions.Expression;
using ExprType = System.Linq.Expressions.ExpressionType;

namespace IronLua.Runtime.Binder
{
    class LuaUnaryOperationBinder : UnaryOperationBinder
    {
        readonly LuaContext context;

        public LuaUnaryOperationBinder(LuaContext context, ExprType operation)
            : base(operation)
        {
            ContractUtils.RequiresNotNull(context, "context");
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
                    expression = NegateOp(target);
                    break;
                case ExprType.Not:
                    expression = NotOp(target);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            return new DynamicMetaObject(Expr.Convert(expression, typeof(object)), RuntimeHelpers.MergeTypeRestrictions(target));
        }

        Expr NegateOp(DynamicMetaObject target)
        {
            var expr = LuaConvertBinder.ToNumber(target);

            if (expr == null)
                return MetamethodFallbacks.UnaryMinus(context, target);

            if (target.LimitType == typeof(string))
                return FallbackIfNumberIsNan(expr);

            return Expr.MakeUnary(Operation, Expr.Convert(expr, typeof(double)), null);
        }

        Expr NotOp(DynamicMetaObject target)
        {
            if (target.LimitType == typeof(bool))
                return Expr.MakeUnary(Operation, target.Expression, null);
            return Expr.Equal(target.Expression, Expr.Constant(null));
        }

        Expr FallbackIfNumberIsNan(Expr numExpr)
        {
            // If we have performed a string to number conversion check that conversion went well by checking
            // number for NaN. If conversion failed do metatable fallback. Also assign to temp variable for single evaluation.

            var numVar = Expr.Variable(typeof(double));

            var expr = Expr.IfThenElse(
                Expr.Invoke(Expr.Constant((Func<double, bool>)Double.IsNaN), numVar),
                Expr.Invoke(
                    Expr.Constant((Func<LuaContext, object, object>)LuaOps.UnaryMinusMetamethod),
                    Expr.Constant(context),
                    Expr.Constant(Operation),
                    Expr.Convert(numExpr, typeof(object))),
                numVar);

            return Expr.Block(
                new[] { numVar },
                Expr.Assign(numVar, numExpr),
                expr);
        }
    }
}