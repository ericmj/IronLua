using System;
using System.Dynamic;

namespace IronLua.Runtime.Binder
{
    class LuaSetMemberBinder : SetMemberBinder
    {
        public LuaSetMemberBinder(string name)
            : base(name, false)
        {
        }

        public override DynamicMetaObject FallbackSetMember(DynamicMetaObject target, DynamicMetaObject value, DynamicMetaObject errorSuggestion)
        {
            throw new InvalidOperationException();
        }
    }
}