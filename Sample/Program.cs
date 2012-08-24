using System;
using System.Diagnostics;
using IronLua.Hosting;
using System.Dynamic;
using System.Collections.Generic;

namespace Sample
{
    class Program
    {
        static void Main(string[] args)
        {
            var engine = Lua.CreateEngine();

            var context = engine.GetLuaContext();
            context.SetGlobalVariable("a", 10);
            
            engine.Execute("a = a * 10; print('a='..a)");

            Console.WriteLine("Resulting a=" + context.FormatObject(context.GetGlobalVariable("a")));

            if (Debugger.IsAttached)
            {
                // Pause for debugger to see console output
                Console.WriteLine();
                Console.Write("Press ENTER to continue..");
                Console.ReadLine();
            }
        }
    }
}
