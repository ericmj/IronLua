using System;
using System.Dynamic;
using Expr = System.Linq.Expressions.Expression;

namespace IronLua.Runtime.Binder
{
    class LuaGetIndexBinder : GetIndexBinder
    {
        Context context;

        public LuaGetIndexBinder(Context context)
            : base(new CallInfo(1))
        {
            this.context = context;
        }

        public override DynamicMetaObject FallbackGetIndex(DynamicMetaObject target, DynamicMetaObject[] indexes, DynamicMetaObject errorSuggestion)
        {
            var expression = Expr.Invoke(
                Expr.Constant((Func<Context, object, object, object>)LuaOps.IndexMetamethod),
                Expr.Constant(context),
                Expr.Convert(target.Expression, typeof(object)),
                Expr.Convert(indexes[0].Expression, typeof(object)));

            return new DynamicMetaObject(expression, BindingRestrictions.Empty);
        }
    }
}