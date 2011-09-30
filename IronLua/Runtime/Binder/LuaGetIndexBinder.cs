using System;
using System.Dynamic;
using Expr = System.Linq.Expressions.Expression;

namespace IronLua.Runtime.Binder
{
    class LuaGetIndexBinder : GetIndexBinder
    {
        readonly Context context;

        public LuaGetIndexBinder(Context context)
            : base(new CallInfo(1))
        {
            this.context = context;
        }

        public override DynamicMetaObject FallbackGetIndex(DynamicMetaObject target, DynamicMetaObject[] indexes, DynamicMetaObject errorSuggestion)
        {
            var expression = MetamethodFallbacks.Index(context, target, indexes);

            return new DynamicMetaObject(expression, BindingRestrictions.Empty);
        }
    }
}