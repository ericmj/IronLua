using System.Dynamic;

namespace IronLua.Runtime.Binder
{
    class LuaSetIndexBinder : SetIndexBinder
    {
        public LuaSetIndexBinder() : base(new CallInfo(1))
        {
        }

        public override DynamicMetaObject FallbackSetIndex(
            DynamicMetaObject target, DynamicMetaObject[] indexes,
            DynamicMetaObject value, DynamicMetaObject errorSuggestion)
        {
            throw new System.NotImplementedException();
        }
    }
}