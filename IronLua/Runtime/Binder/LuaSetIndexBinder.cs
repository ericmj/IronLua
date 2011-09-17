using System;
using System.Dynamic;
using Expr = System.Linq.Expressions.Expression;

namespace IronLua.Runtime.Binder
{
    class LuaSetIndexBinder : SetIndexBinder
    {
        readonly Context context;

        public LuaSetIndexBinder(Context context)
            : base(new CallInfo(1))
        {
            this.context = context;
        }

        public override DynamicMetaObject FallbackSetIndex(DynamicMetaObject target, DynamicMetaObject[] indexes, DynamicMetaObject value, DynamicMetaObject errorSuggestion)
        {
            var expression = Expr.Invoke(
                Expr.Constant((Func<Context, object, object, object, object>)LuaOps.NewIndexMetamethod),
                Expr.Constant(context),
                Expr.Convert(target.Expression, typeof(object)),
                Expr.Convert(indexes[0].Expression, typeof(object)),
                Expr.Convert(value.Expression, typeof(object)));

            return new DynamicMetaObject(expression, BindingRestrictions.Empty);
        }
    }
}