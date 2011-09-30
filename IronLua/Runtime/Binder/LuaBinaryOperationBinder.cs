using System;
using System.Collections.Generic;
using System.Dynamic;
using Expr = System.Linq.Expressions.Expression;
using ParamExpr = System.Linq.Expressions.ParameterExpression;
using ExprType = System.Linq.Expressions.ExpressionType;

namespace IronLua.Runtime.Binder
{
    class LuaBinaryOperationBinder : BinaryOperationBinder
    {
        static readonly Dictionary<ExprType, BinaryOpType> binaryExprTypes =
            new Dictionary<ExprType, BinaryOpType>
                {
                    {ExprType.Equal,              BinaryOpType.Relational},
                    {ExprType.NotEqual,           BinaryOpType.Relational},
                    {ExprType.LessThan,           BinaryOpType.Relational},
                    {ExprType.GreaterThan,        BinaryOpType.Relational},
                    {ExprType.LessThanOrEqual,    BinaryOpType.Relational},
                    {ExprType.GreaterThanOrEqual, BinaryOpType.Relational},
                    {ExprType.OrElse,             BinaryOpType.Logical},
                    {ExprType.AndAlso,            BinaryOpType.Logical},
                    {ExprType.Add,                BinaryOpType.Numeric},
                    {ExprType.Subtract,           BinaryOpType.Numeric},
                    {ExprType.Multiply,           BinaryOpType.Numeric},
                    {ExprType.Divide,             BinaryOpType.Numeric},
                    {ExprType.Modulo,             BinaryOpType.Numeric},
                    {ExprType.Power,              BinaryOpType.Numeric}
                };

        readonly Context context;

        public LuaBinaryOperationBinder(Context context, ExprType op)
            : base(op)
        {
            this.context = context;
        }

        public override DynamicMetaObject FallbackBinaryOperation(DynamicMetaObject target, DynamicMetaObject arg, DynamicMetaObject errorSuggestion)
        {
            if (!target.HasValue || !arg.HasValue)
                return Defer(target, arg);

            DynamicMetaObject targetFirst, argFirst;
            if (RuntimeHelpers.TryGetFirstVarargs(target, out targetFirst) | RuntimeHelpers.TryGetFirstVarargs(arg, out argFirst))
                return FallbackBinaryOperation(targetFirst, argFirst, errorSuggestion);

            Expr expression = null;
            switch (binaryExprTypes[Operation])
            {
                case BinaryOpType.Relational:
                    expression = Relational(target, arg);
                    break;
                case BinaryOpType.Logical:
                    expression = Logical(target, arg);
                    break;
                case BinaryOpType.Numeric:
                    expression = Numeric(target, arg);
                    break;
            }

            if (expression == null)
                expression = MetamethodFallbacks.BinaryOp(context, Operation, target, arg);

            return new DynamicMetaObject(Expr.Convert(expression, typeof(object)), RuntimeHelpers.MergeTypeRestrictions(target, arg));
        }

        Expr Relational(DynamicMetaObject left, DynamicMetaObject right)
        {
            if (left.LimitType == typeof(bool) && right.LimitType == typeof(bool))
            {
                if (!(Operation == ExprType.Equal || Operation == ExprType.NotEqual))
                    return null;
            }

            return Expr.MakeBinary(
                Operation,
                Expr.Convert(left.Expression, left.LimitType),
                Expr.Convert(right.Expression, right.LimitType));
        }

        Expr Logical(DynamicMetaObject left, DynamicMetaObject right)
        {
            // Assign left operand to a temp variable for single evaluation
            var tempLeft = Expr.Variable(left.LimitType);

            var compareExpr = (Expr)tempLeft;
            Expr ifExpr = null;

            switch (Operation)
            {
                case ExprType.AndAlso:
                    if (left.LimitType != typeof(bool))
                        compareExpr = Expr.Equal(tempLeft, Expr.Constant(null));

                    ifExpr = Expr.IfThenElse(compareExpr, right.Expression, tempLeft);
                    break;

                case ExprType.OrElse:
                    if (left.LimitType != typeof(bool))
                        compareExpr = Expr.NotEqual(tempLeft, Expr.Constant(null));

                    ifExpr = Expr.IfThenElse(compareExpr, tempLeft, right.Expression);
                    break;
            }

            return
                Expr.Block(
                    new[] { tempLeft },
                    Expr.Assign(tempLeft, left.Expression),
                    ifExpr);
        }

        Expr Numeric(DynamicMetaObject left, DynamicMetaObject right)
        {
            var leftExpr = LuaConvertBinder.ToNumber(left);
            var rightExpr = LuaConvertBinder.ToNumber(right);

            if (leftExpr == null)
                return null;

            if (left.LimitType == typeof(string))
                return FallbackIfNumberIsNan(leftExpr, left, right);

            return Expr.Convert(Expr.MakeBinary(Operation, leftExpr, rightExpr), typeof(object));
        }

        Expr FallbackIfNumberIsNan(Expr conversionResult, DynamicMetaObject left, DynamicMetaObject right)
        {
            // If we have performed a string to number conversion check that conversion went well by checking
            // number for NaN. If conversion failed do metatable fallback. Also assign to temp variable for single evaluation.

            var conversionVar = Expr.Variable(typeof(double));

            var expr = Expr.Condition(
                Expr.Invoke(Expr.Constant((Func<double, bool>)Double.IsNaN), conversionVar),
                Expr.Invoke(
                    Expr.Constant((Func<Context, ExprType, object, object, object>)LuaOps.NumericMetamethod),
                    Expr.Constant(context),
                    Expr.Constant(Operation),
                    Expr.Convert(left.Expression, typeof(object)),
                    Expr.Convert(right.Expression, typeof(object))),
                Expr.Convert(conversionVar, typeof(object)));

            return Expr.Block(
                new[] {conversionVar},
                Expr.Assign(conversionVar, conversionResult),
                expr);
        }

        enum BinaryOpType
        {
            Relational,
            Logical,
            Numeric
        }
    }
}