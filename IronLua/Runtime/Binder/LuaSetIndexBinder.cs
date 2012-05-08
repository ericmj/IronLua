using System.Diagnostics.Contracts;
using System.Dynamic;

namespace IronLua.Runtime.Binder
{
    class LuaSetIndexBinder : SetIndexBinder
    {
        private readonly LuaContext _context;

        public LuaSetIndexBinder(LuaContext context, CallInfo callInfo)
            : base(callInfo)
        {
            Contract.Requires(context != null);
            _context = context;
        }

        public LuaSetIndexBinder(LuaContext context)
            : this(context, new CallInfo(1))
        {
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