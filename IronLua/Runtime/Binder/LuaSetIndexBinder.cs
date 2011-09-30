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
            var expression = MetamethodFallbacks.NewIndex(context, target, indexes, value);

            return new DynamicMetaObject(expression, BindingRestrictions.Empty);
        }
    }
}