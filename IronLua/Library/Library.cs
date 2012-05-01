using IronLua.Runtime;
using Microsoft.Scripting.Utils;

namespace IronLua.Library
{
    abstract class Library
    {
        protected LuaContext Context { get; private set; }

        protected Library(LuaContext context)
        {
            ContractUtils.RequiresNotNull(context, "context");
            Context = context;
        }

        public abstract void Setup(LuaTable table);
    }
}
