using System;
using System.Dynamic;

namespace IronLua.Runtime.Binder
{
    class LuaGetMemberBinder : GetMemberBinder
    {
        private readonly LuaContext _context;

        [Obsolete("TODO: Need to fix this to pass in a LuaContext")]
        public LuaGetMemberBinder(string name)
            : this(null, name)
        {
            // TODO: need to fix LuaTable, does not have a Context
        }

        public LuaGetMemberBinder(LuaContext context, string name)
            : base(name, false)
        {
            _context = context;
        }

        public LuaContext Context
        {
            get { return _context; }
        }

        public override DynamicMetaObject FallbackGetMember(DynamicMetaObject target, DynamicMetaObject errorSuggestion)
        {
            throw new NotImplementedException();
        }
    }
}