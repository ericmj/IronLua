using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using IronLua.Runtime.Binder;

namespace IronLua.Runtime
{
    class Enviroment
    {
        LuaTable globals;

        public BinderCache BinderCache { get; private set; }

        public Enviroment()
        {
            BinderCache = new BinderCache();
        }
    }
}
