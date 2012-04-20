using Microsoft.Scripting.Actions;
using Microsoft.Scripting.Utils;

namespace IronLua.Runtime.Binder
{
    class LuaBinder : DefaultBinder
    {
        readonly LuaContext _context;

        public LuaBinder(LuaContext luaContext)
        {
            ContractUtils.RequiresNotNull(luaContext, "luaContext");

            _context = luaContext;
        }

        public LuaBinder(LuaBinder binder)
        {
            _context = binder._context;
        }
    }
}