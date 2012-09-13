using System;
using System.Text;
using IronLua.Runtime;

namespace IronLua.Library
{
    class PackageLibrary : Library
    {
        static readonly string ConfigStr = new StringBuilder()
            .AppendLine("\\")
            .AppendLine(";")
            .AppendLine("?")
            .AppendLine("!")
            .AppendLine("-")
            .ToString();

        public PackageLibrary(LuaContext context) 
            : base(context)
        {
        }

        public static object Loadlib(string libName, string funcName)
        {
            throw new NotImplementedException();   
        }

        public static object SearchPath(string name, string path, string sep, string rep)
        {
            throw new NotImplementedException();    
        }

        public override void Setup(LuaTable table)
        {
            table.SetConstant("config", ConfigStr);

            table.SetConstant("cpath", 
                Environment.GetEnvironmentVariable("LUA_CPATH_5_2") ??
                    Environment.GetEnvironmentVariable("LUA_CPATH") ??
                        String.Join(";", new[]
                        {
                            "!\\?.dll", 
                            "!\\loadall.dll", 
                            ".\\?.dll"
                        }));

            table.SetConstant("path",
                Environment.GetEnvironmentVariable("LUA_PATH_5_2") ??
                    Environment.GetEnvironmentVariable("LUA_PATH") ??
                        String.Join(";", new[] 
                        { 
                            "!\\lua\\" + "?.lua",
                            "!\\lua\\" + "?\\init.lua",
                            "!\\" + "?.lua",
                            "!\\" + "?\\init.lua",
                            ".\\?.lua"
                        }));

            table.SetConstant("loaded", new LuaTable(Context));
            table.SetConstant("preload", new LuaTable(Context));
            table.SetConstant("searchers", new LuaTable(Context)); // TODO: fill with searchers

            table.SetConstant("loadlib", (Func<string, string, object>)Loadlib);
            table.SetConstant("searchpath", (Func<string, string, string, string, object>) SearchPath);
        }
    }
}