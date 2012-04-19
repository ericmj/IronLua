using System;
using System.Reflection;
using IronLua.Runtime;
using Microsoft.Scripting.Hosting.Shell;

namespace IronLua.Hosting
{
    public class LuaCommandLine : CommandLine
    {
        public LuaCommandLine()
        {
        }

        protected override string Logo
        {
            get
            {
                return String.Format(
                    "IronLua {1} on {2}{0}Copyright (C) 1994-2008 Lua.org, PUC-Rio{0}",
                    Environment.NewLine, 
                    // /*LuaContext.LuaLanguageVersion*/ new Version(0,0,1), 
                    LuaContext.GetLuaVersion(),
                    GetRuntimeInfo());
            }
        }

        private string GetRuntimeInfo()
        {
            var mono = typeof(object).Assembly.GetType("Mono.Runtime");
            if (mono != null)
            {
                return (string)mono.GetMethod("GetDisplayName", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, null);
            }
            return string.Format(".NET {0}", Environment.Version);
        }

        protected override string Prompt
        {
            get { return @"> "; }
        }

        public override string PromptContinuation
        {
            get { return @">> "; }
        }
    }
}