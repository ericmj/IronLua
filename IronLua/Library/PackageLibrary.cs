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
            table.SetValue("config", ConfigStr);

            table.SetValue("cpath", 
                Environment.GetEnvironmentVariable("LUA_CPATH_5_2") ??
                    Environment.GetEnvironmentVariable("LUA_CPATH") ??
                        String.Join(";", new[]
                        {
                            "!\\?.dll", 
                            "!\\loadall.dll", 
                            ".\\?.dll"
                        }));

            table.SetValue("path",
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

            table.SetValue("loaded", new LuaTable());
            table.SetValue("preload", new LuaTable());
            table.SetValue("searchers", new LuaTable()); // TODO: fill with searchers

            table.SetValue("loadlib", (Func<string, string, object>)Loadlib);
            table.SetValue("searchpath", (Func<string, string, string, string, object>) SearchPath);
        }
    }
}