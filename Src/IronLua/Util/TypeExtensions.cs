using System;

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
