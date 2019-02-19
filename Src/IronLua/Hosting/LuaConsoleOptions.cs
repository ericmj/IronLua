using Microsoft.Scripting.Hosting.Shell;

namespace IronLua.Hosting
{
    public class LuaConsoleOptions : ConsoleOptions
    {
        public LuaConsoleOptions()
        {
        }

        protected LuaConsoleOptions(ConsoleOptions options)
            : base(options)
        {
            var luaOptions = options as LuaConsoleOptions;
            if (luaOptions != null)
            {
                SkipFirstSourceLine = luaOptions.SkipFirstSourceLine;
            }
        }

        /// <summary>
        /// Skip the first line of the code to execute. This is useful for executing Unix scripts which
        ///             have the command to execute specified in the first line.
        ///             This only apply to the script code executed by the ScriptEngine APIs, but not for other script code
        ///             that happens to get called as a result of the execution.
        /// 
        /// </summary>
        public bool SkipFirstSourceLine
        {
            get; set;
        }        
    }
}