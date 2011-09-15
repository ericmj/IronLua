using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IronLua.Util
{
    static class TypeExtensions
    {
        public static object GetDefaultValue(this Type type)
        {
            return type.IsValueType ? Activator.CreateInstance(type) : null;
        }
    }
}
