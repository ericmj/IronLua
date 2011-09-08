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
            else
                throw new Exception(); // TODO: Use errorSuggestion

            if (expression == null)
                throw new Exception(); // TODO: Use errorSuggestion

            return new DynamicMetaObject(expression, target.MergeTypeRestrictions());
        }

        public static Expression ToNumber(DynamicMetaObject metaObject)
        {
            if (metaObject.LimitType == typeof(double))
                return Expr.Convert(metaObject.Expression, typeof(double));
            if (metaObject.LimitType == typeof(string))
                return
                    Expression.Invoke(
                        Expression.Constant((Func<string, double, double>) Global.InternalToNumber),
                        metaObject.Expression, Expression.Constant(10.0, typeof(double?)));

            return null;
        }
    }
}