using System;
using IronLua.Runtime;
using Microsoft.Scripting.Hosting.Shell;

namespace IronLua.Hosting
{
    public class LuaConsoleHost : ConsoleHost
    {
        protected override Type Provider
        {
            get { return typeof(LuaContext); }
        }

        protected override CommandLine CreateCommandLine()
        {
            return new LuaCommandLine();
        }

        protected override OptionsParser CreateOptionsParser()
        {
            return new LuaOptionsParser();
        }        
    }
}