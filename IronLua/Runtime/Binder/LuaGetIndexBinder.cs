using System.Dynamic;
using Microsoft.Scripting.Utils;
using Expr = System.Linq.Expressions.Expression;

namespace IronLua.Runtime.Binder
{
    class LuaGetIndexBinder : GetIndexBinder
    {
        readonly LuaContext context;

        public LuaGetIndexBinder(LuaContext context)
            : base(new CallInfo(1))
        {
            ContractUtils.RequiresNotNull(context, "context");
            this.context = context;
        }

        public override DynamicMetaObject FallbackGetIndex(DynamicMetaObject target, DynamicMetaObject[] indexes, DynamicMetaObject errorSuggestion)
        {
            var expression = MetamethodFallbacks.Index(context, target, indexes);

            return new DynamicMetaObject(expression, BindingRestrictions.Empty);
        }
    }
}