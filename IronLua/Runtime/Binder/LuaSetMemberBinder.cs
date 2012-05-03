using System;
using System.Diagnostics.Contracts;
using System.Dynamic;

namespace IronLua.Runtime.Binder
{
    class LuaSetMemberBinder : SetMemberBinder
    {
        private readonly LuaContext _context;

        public LuaSetMemberBinder(LuaContext context, string name)
            : base(name, false)
        {
            Contract.Requires(context != null);
            _context = context;
        }

        public LuaContext Context
        {
            get { return _context; }
        }

        public override DynamicMetaObject FallbackSetMember(DynamicMetaObject target, DynamicMetaObject value, DynamicMetaObject errorSuggestion)
        {
            throw new NotImplementedException();
        }
    }
}