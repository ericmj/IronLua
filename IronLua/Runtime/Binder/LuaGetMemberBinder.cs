using System;
using System.Diagnostics.Contracts;
using System.Dynamic;

namespace IronLua.Runtime.Binder
{
    class LuaGetMemberBinder : GetMemberBinder
    {
        private readonly LuaContext _context;

        public LuaGetMemberBinder(LuaContext context, string name, bool ignoreCase = false)
            : base(name, ignoreCase)
        {
            Contract.Requires(context != null);
            _context = context;
        }

        public LuaContext Context
        {
            get { return _context; }
        }

        public override DynamicMetaObject FallbackGetMember(
            DynamicMetaObject target, 
            DynamicMetaObject errorSuggestion)
        {
            // Defer if any object has no value so that we evaulate their
            // Expressions and nest a CallSite for the InvokeMember.
            if (!target.HasValue) 
                return Defer(target);

            _context.Binder.GetMember("__get", target);

            throw new NotImplementedException();
        }
    }
}