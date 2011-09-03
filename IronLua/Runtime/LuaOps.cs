using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IronLua.Runtime
{
    static class LuaOps
    {
        internal static bool Not(object value)
        {
            return value == null || (value is bool && !(bool)value);
        }
    }
}
