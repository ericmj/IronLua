using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Globalization;
using System.Reflection;
using IronLua.Library;
using Expr = System.Linq.Expressions.Expression;
using ExprType = System.Linq.Expressions.ExpressionType;

namespace IronLua.Runtime.Binder
{
    class LuaBinaryOperationBinder : BinaryOperationBinder
    {
        static Dictionary<ExprType, BinaryOpType> binaryExprTypes =
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

        Context context;

        public LuaBinaryOperationBinder(Context context, ExprType op)
            : base(op)
        {
            this.context = context;
        }

        public override DynamicMetaObject FallbackBinaryOperation(DynamicMetaObject target, DynamicMetaObject arg, DynamicMetaObject errorSuggestion)
        {
            if (!target.HasValue || !arg.HasValue)
                return Defer(target, arg);

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
                expression = MetamethodFallback(target, arg);

            return new DynamicMetaObject(Expr.Convert(expression, typeof(object)), TypeRestrictions(target, arg));
        }

        Expr Relational(DynamicMetaObject left, DynamicMetaObject right)
        {
            if (left.LimitType == typeof(bool) && right.LimitType == typeof(bool))
            {
                if (!(Operation == ExprType.Equal || Operation == ExprType.NotEqual))
                    return null;

                return
                    Expr.MakeBinary(
                        Operation,
                        Expr.Convert(left.Expression, left.LimitType),
                        Expr.Convert(right.Expression, right.LimitType));
            }

            if (left.LimitType == typeof(double) && right.LimitType == typeof(double))
            {
                return
                    Expr.MakeBinary(
                        Operation,
                        Expr.Convert(left.Expression, left.LimitType),
                        Expr.Convert(right.Expression, right.LimitType));
            }

            if (left.LimitType == typeof(string) && right.LimitType == typeof(string))
            {
                var compareExpr =
                    Expr.Invoke(
                        Expr.Constant((Func<string, string, StringComparison, int>)String.Compare),
                        left.Expression,
                        right.Expression,
                        Expr.Constant(StringComparison.InvariantCulture));

                return
                    Expr.MakeBinary(
                        Operation,
                        compareExpr,
                        Expr.Constant(0));
            }

            return Expr.Constant(false);
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
                        compareExpr = Expr.MakeBinary(ExprType.Equal, tempLeft, Expr.Constant(null));

                    ifExpr = Expr.IfThenElse(compareExpr, right.Expression, tempLeft);
                    break;

                case ExprType.OrElse:
                    if (left.LimitType != typeof(bool))
                        compareExpr = Expr.MakeBinary(ExprType.NotEqual, tempLeft, Expr.Constant(null));

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
            var leftExpr = ConvertToNumberOperand(left);
            var rightEXpr = ConvertToNumberOperand(right);

            if (left == null)
                return null;
            return Expr.Convert(Expr.MakeBinary(Operation, leftExpr, rightEXpr), typeof(object));
        }

        Expr MetamethodFallback(DynamicMetaObject left, DynamicMetaObject right)
        {
            return Expr.Call(
                Expr.Constant(context),
                typeof(Context).GetMethod("BinaryOpMetamethod",
                                             BindingFlags.NonPublic |
                                             BindingFlags.InvokeMethod |
                                             BindingFlags.Instance),
                Expr.Constant(Operation),
                Expr.Convert(left.Expression, typeof(object)),
                Expr.Convert(right.Expression, typeof(object)));
        }

        static Expr ConvertToNumberOperand(DynamicMetaObject metaObject)
        {
            if (metaObject.LimitType == typeof(double))
                return metaObject.Expression;
            if (metaObject.LimitType == typeof(string))
                return
                    Expr.Invoke(
                        Expr.Constant((Func<string, int?, double>) Global.ToNumber),
                        metaObject.Expression, Expr.Constant(10, typeof(int?)));

            return null;
        }

        // TODO?: Convert to extension method on BindingRestrictions
        static BindingRestrictions TypeRestrictions(DynamicMetaObject target, DynamicMetaObject arg)
        {
            return
                target.Restrictions
                    .Merge(arg.Restrictions)
                    .Merge(BindingRestrictions.GetTypeRestriction(target.Expression, target.LimitType))
                    .Merge(BindingRestrictions.GetTypeRestriction(arg.Expression, arg.LimitType));
        }

        enum BinaryOpType
        {
            Relational,
            Logical,
            Numeric
        }
    }
}