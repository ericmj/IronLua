using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq.Expressions;
using IronLua.Library;
using Expr = System.Linq.Expressions.Expression;
using ExprType = System.Linq.Expressions.ExpressionType;

namespace IronLua.Runtime.Binder
{
    class LuaBinaryOperationBinder : BinaryOperationBinder
    {
        static HashSet<ExprType> numericExpressionTypes =
            new HashSet<ExprType>
                {
                    ExprType.Add,
                    ExprType.Subtract,
                    ExprType.Multiply,
                    ExprType.Divide,
                    ExprType.Modulo,
                    ExprType.Power
                };

        Enviroment enviroment;

        public LuaBinaryOperationBinder(Enviroment enviroment, ExprType op)
            : base(op)
        {
            this.enviroment = enviroment;
        }

        public override DynamicMetaObject FallbackBinaryOperation(DynamicMetaObject target, DynamicMetaObject arg, DynamicMetaObject errorSuggestion)
        {
            // TODO: Check metatables if ConvertOperand() returns null (Dictionary<Type,LuaTable> in enviroment)

            if (!target.HasValue || !arg.HasValue)
                return Defer(target, arg);

            var restrictions =
                target.Restrictions
                    .Merge(arg.Restrictions)
                    .Merge(BindingRestrictions.GetTypeRestriction(target.Expression, target.LimitType))
                    .Merge(BindingRestrictions.GetTypeRestriction(arg.Expression, arg.LimitType));

            var left = target.Expression;
            var right = arg.Expression;
            if (numericExpressionTypes.Contains(Operation))
            {
                left = ConvertToNumberOperand(target);
                right = ConvertToNumberOperand(arg);
            }

            var result = Expr.Convert(Expr.MakeBinary(Operation, left, right), typeof(object));

            return new DynamicMetaObject(result, restrictions);
        }

        Expr ConvertToNumberOperand(DynamicMetaObject metaObject)
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
    }
}