using System;
using System.Dynamic;

namespace IronLua.Runtime.Binder
{
    class LuaGetMemberBinder : GetMemberBinder
    {
        public LuaGetMemberBinder(string name)
            : base(name, false)
        {
        }

        public override DynamicMetaObject FallbackGetMember(DynamicMetaObject target, DynamicMetaObject errorSuggestion)
        {
            throw new InvalidOperationException();
        }
    }
}