using System;
using System.Dynamic;
using Microsoft.Scripting.Utils;
using Expr = System.Linq.Expressions.Expression;

namespace IronLua.Runtime.Binder
{
    class LuaSetIndexBinder : SetIndexBinder
    {
        private readonly LuaContext _context;

        public LuaSetIndexBinder(LuaContext context)
            : base(new CallInfo(1))
        {
            ContractUtils.RequiresNotNull(context, "context");
            _context = context;
        }

        public LuaContext Context
        {
            get { return _context; }
        }
        
        public override DynamicMetaObject FallbackSetIndex(DynamicMetaObject target, DynamicMetaObject[] indexes, DynamicMetaObject value, DynamicMetaObject errorSuggestion)
        {
            var expression = MetamethodFallbacks.NewIndex(_context, target, indexes, value);

            return new DynamicMetaObject(expression, BindingRestrictions.Empty);
        }
    }
}