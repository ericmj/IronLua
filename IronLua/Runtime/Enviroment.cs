using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using IronLua.Runtime.Binder;

namespace IronLua.Runtime
{
    class Enviroment
    {
        BinderCache binderCache;

        public Enviroment()
        {
            binderCache = new BinderCache();
        }
    }
}
