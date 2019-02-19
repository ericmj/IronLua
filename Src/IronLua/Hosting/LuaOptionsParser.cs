using Microsoft.Scripting.Hosting.Shell;
using Microsoft.Scripting.Utils;

namespace IronLua.Hosting
{
    public sealed class LuaOptionsParser : OptionsParser<ConsoleOptions>
    {

        protected override void ParseArgument(string arg)
        {
            ContractUtils.RequiresNotNull(arg, "arg");

            switch (arg)
            {
                default:
                    base.ParseArgument(arg);
                    break;
            }
        }

        public override void GetHelp(out string commandLine, out string[,] options, out string[,] environmentVariables, out string comments)
        {
            string[,] standardOptions;
            base.GetHelp(out commandLine, out standardOptions, out environmentVariables, out comments);

            var luaOptions = new[,] {
                //{ "-e stat", "execute string 'stat'" }, 
                { "-l name", "require library 'name'" }, 
                //{ "-i", "enter interactive mode after executing 'script'" }, 
                //{ "-v", "show version information" },
                //{ "--", "stop handling options" },
                //{ "-", "execute stdin and stop handling options"},
            };

            // Append the Lua-specific options and the standard options
            options = ArrayUtils.Concatenate(luaOptions, standardOptions);
        }
    }
}