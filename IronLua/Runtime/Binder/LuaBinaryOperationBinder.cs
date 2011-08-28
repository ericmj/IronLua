using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Globalization;
using System.Linq.Expressions;
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

        Enviroment enviroment;

        public LuaBinaryOperationBinder(Enviroment enviroment, ExprType op)
            : base(op)
        {
            this.enviroment = enviroment;
        }

        public override DynamicMetaObject FallbackBinaryOperation(DynamicMetaObject target, DynamicMetaObject arg, DynamicMetaObject errorSuggestion)
        {
            if (!target.HasValue || !arg.HasValue)
                return Defer(target, arg);

            switch (binaryExprTypes[Operation])
            {
                case BinaryOpType.Relational:
                    return Relational(target, arg, errorSuggestion);
                case BinaryOpType.Logical:
                    return Logical(target, arg, errorSuggestion);
                case BinaryOpType.Numeric:
                    return Numeric(target, arg, errorSuggestion);
            }

            throw new Exception();
        }

        DynamicMetaObject Relational(DynamicMetaObject left, DynamicMetaObject right, DynamicMetaObject errorSuggestion)
        {
            Expr expression;

            if (left.LimitType == typeof(double) && right.LimitType == typeof(double))
            {
                expression =
                    Expr.MakeBinary(
                        Operation,
                        Expr.Convert(left.Expression, left.LimitType),
                        Expr.Convert(right.Expression, right.LimitType));
            }
            else if (left.LimitType == typeof(double) && right.LimitType == typeof(double))
            {
                var compareExpr =
                    Expr.Invoke(
                        Expr.Constant((Func<string, string, StringComparison, int>)String.Compare),
                        left.Expression,
                        right.Expression,
                        Expr.Constant(StringComparison.InvariantCulture));

                expression =
                    Expr.MakeBinary(
                        Operation,
                        compareExpr,
                        Expr.Constant(0));
            }
            else
            {
                // TODO: METATABLE
                expression = null;
            }

            return new DynamicMetaObject(Expr.Convert(expression, typeof(object)), TypeRestrictions(left, right));
        }

        DynamicMetaObject Logical(DynamicMetaObject left, DynamicMetaObject right, DynamicMetaObject errorSuggestion)
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

            Expr expression =
                Expr.Block(
                    new[] { tempLeft },
                    Expr.Assign(tempLeft, left.Expression),
                    ifExpr);

            return new DynamicMetaObject(Expr.Convert(expression, typeof(object)), TypeRestrictions(left, right));
        }

        DynamicMetaObject Numeric(DynamicMetaObject left, DynamicMetaObject right, DynamicMetaObject errorSuggestion)
        {
            var leftExpr = ConvertToNumberOperand(left);
            var rightEXpr = ConvertToNumberOperand(right);
            var expression = Expr.Convert(Expr.MakeBinary(Operation, leftExpr, rightEXpr), typeof(object));

            if (left != null)
                return new DynamicMetaObject(expression, TypeRestrictions(left, right));
            
            // TODO: METATABLE
            return null;
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