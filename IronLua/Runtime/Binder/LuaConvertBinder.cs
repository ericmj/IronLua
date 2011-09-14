using System;
using System.Dynamic;
using System.Linq.Expressions;
using IronLua.Library;
using Expr = System.Linq.Expressions.Expression;
using IronLua.Util;

namespace IronLua.Runtime.Binder
{
    class LuaConvertBinder : ConvertBinder
    {
        public LuaConvertBinder(Type type)
            : base(type, false)
        {
        }

        public override DynamicMetaObject FallbackConvert(DynamicMetaObject target, DynamicMetaObject errorSuggestion)
        {
            Expr expression;
            if (Type == typeof(double))
                expression = ToNumber(target);
            else if (Type == typeof(bool))
                expression = ToBool(target);
            else
                throw new Exception(); // TODO: Use errorSuggestion

            if (expression == null)
                throw new Exception(); // TODO: Use errorSuggestion

            return new DynamicMetaObject(expression, RuntimeHelpers.MergeTypeRestrictions(target));
        }

        static Expression ToBool(DynamicMetaObject target)
        {
            if (target.LimitType == typeof(bool))
                return Expr.Convert(target.Expression, typeof(bool));
            return Expr.NotEqual(target.Expression, Expr.Constant(null));
        }

        public static Expression ToNumber(DynamicMetaObject target)
        {
            if (target.LimitType == typeof(double))
                return Expr.Convert(target.Expression, typeof(double));
            if (target.LimitType == typeof(string))
                return
                    Expression.Invoke(
                        Expression.Constant((Func<string, double, double>) Global.InternalToNumber),
                        target.Expression, Expression.Constant(10.0));

            return null;
        }
    }
}