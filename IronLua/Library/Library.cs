using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using IronLua.Runtime;

namespace IronLua.Library
{
    abstract class Library
    {
        protected Context Context { get; private set; }

        protected Library(Context context)
        {
            Context = context;
        }

        public abstract void Setup(LuaTable table);
    }
}
