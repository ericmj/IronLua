using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using IronLua.Runtime;

namespace IronLua
{
    // TODO: Automate testing of all fields
    static class Methods
    {
        public static readonly ConstructorInfo NewVarargs =
            typeof(Varargs).GetConstructor(new[] {typeof(object[])});

        public static readonly ConstructorInfo NewLuaTable =
            typeof(LuaTable).GetConstructor(new Type[0]);

        public static readonly MethodInfo LuaTableSetValue =
            typeof(LuaTable).GetMethod("SetValue", BindingFlags.NonPublic | BindingFlags.Instance);

        public static readonly MethodInfo LuaTableGetValue =
            typeof(LuaTable).GetMethod("GetValue", BindingFlags.NonPublic | BindingFlags.Instance);

        public static readonly MethodInfo VarargsFirst =
            typeof(Varargs).GetMethod("First");

        public static readonly MethodInfo ObjectToString =
            typeof(object).GetMethod("ToString");

        public static ConstructorInfo NewRuntimeException =
            typeof(LuaRuntimeException).GetConstructor(new[] { typeof(string), typeof(object[]) });
    }
}
