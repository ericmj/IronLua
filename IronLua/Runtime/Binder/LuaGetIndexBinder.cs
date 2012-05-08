using System.Diagnostics.Contracts;
using System.Dynamic;

namespace IronLua.Runtime.Binder
{
    class LuaGetIndexBinder : GetIndexBinder
    {
        private readonly LuaContext _context;

        public LuaGetIndexBinder(LuaContext context, CallInfo callInfo)
            : base(callInfo)
        {
            Contract.Requires(context != null);
            _context = context;
        }

        public LuaGetIndexBinder(LuaContext context)
            : this(context, new CallInfo(1))
        {            
        }

        public LuaContext Context
        {
            get { return _context; }
        }
        
        public override DynamicMetaObject FallbackGetIndex(DynamicMetaObject target, DynamicMetaObject[] indexes, DynamicMetaObject errorSuggestion)
        {
            var expression = MetamethodFallbacks.Index(_context, target, indexes);

            return new DynamicMetaObject(expression, BindingRestrictions.Empty);
        }
    }
}