using System;
using IronLua.Runtime;
using Microsoft.Scripting.Hosting;

namespace IronLua.Hosting
{
    public sealed class LuaService : MarshalByRefObject
    {
        private readonly ScriptEngine _engine;
        private readonly LuaContext _context;

        public LuaService(LuaContext context, ScriptEngine engine)
        {
            _context = context;
            _engine = engine;
        }

        public override object InitializeLifetimeService()
        {            
            return _engine.InitializeLifetimeService();
        }
    }
}