using System;
using System.Dynamic;

namespace IronLua.Runtime.Binder
{
    class LuaGetIndexBinder : GetIndexBinder
    {
        public LuaGetIndexBinder(CallInfo callInfo) : base(callInfo)
        {
            throw new NotImplementedException();
        }

        public override DynamicMetaObject FallbackGetIndex(DynamicMetaObject target, DynamicMetaObject[] indexes, DynamicMetaObject errorSuggestion)
        {
            throw new NotImplementedException();
        }
    }
}