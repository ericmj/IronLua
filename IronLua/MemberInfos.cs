using System;
using System.Reflection;
using IronLua.Runtime;

namespace IronLua
{
    // TODO: Automate testing of all fields
    static class MemberInfos
    {
        public static readonly ConstructorInfo NewVarargs =
            typeof(Varargs).GetConstructor(new[] {typeof(object[])});

        public static readonly ConstructorInfo NewLuaTable =
            typeof(LuaTable).GetConstructor(new Type[] { typeof(LuaContext) });

        public static readonly MethodInfo LuaTableSetValue =
            typeof(LuaTable).GetMethod("SetValue", BindingFlags.NonPublic | BindingFlags.Instance);

        public static readonly MethodInfo LuaTableGetValue =
            typeof(LuaTable).GetMethod("GetValue", BindingFlags.NonPublic | BindingFlags.Instance);

        public static readonly MethodInfo VarargsFirst =
            typeof(Varargs).GetMethod("First");

        public static readonly MethodInfo ObjectToString =
            typeof(object).GetMethod("ToString");

        public static readonly ConstructorInfo NewRuntimeException =
            typeof(LuaRuntimeException).GetConstructor(new[] { typeof(LuaContext), typeof(string), typeof(object[]) });

        public static readonly FieldInfo LuaTableMetatable =
            typeof(LuaTable).GetField("Metatable");
    }
}
