using System;
using System.Text;
using System.Windows.Forms;
using IronLua.Hosting;

namespace IronLua
{
    public class LuaWindowConsoleHost : LuaConsoleHost 
    {
        protected override string GetHelp()
        {
            var sb = new StringBuilder();

            sb.AppendLine(LuaCommandLine.GetLogoDisplay());
            PrintLanguageHelp(sb);
            sb.AppendLine();

            return sb.ToString();
        }

        protected override void PrintHelp()
        {
            MessageBox.Show(GetHelp(), "IronLua Window Console Help");
        }

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static int Main(string[] args)
        {
            if (args.Length == 0)
            {
                new LuaWindowConsoleHost().PrintHelp();
                return 1;
            }

            return new LuaWindowConsoleHost().Run(args);
        }
    }
}
