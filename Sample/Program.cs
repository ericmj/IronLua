using System;
using System.Diagnostics;
using IronLua.Hosting;

namespace Sample
{
    class Program
    {
        static void Main(string[] args)
        {
            Lua.CreateEngine().Execute("print('hello world')");

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
