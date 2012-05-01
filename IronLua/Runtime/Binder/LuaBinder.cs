using Microsoft.Scripting.Actions;
using Microsoft.Scripting.Utils;

namespace IronLua.Runtime.Binder
{
    class LuaBinder : DefaultBinder
    {
        readonly LuaContext _context;

        public LuaBinder(LuaContext context)
        {
            ContractUtils.RequiresNotNull(context, "context");

            _context = context;
        }

        public LuaBinder(LuaBinder binder)
        {
            _context = binder._context;
        }
    }
}