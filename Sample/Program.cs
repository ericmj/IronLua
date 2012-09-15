using System;
using System.Diagnostics;
using IronLua.Hosting;
using System.Dynamic;
using System.Collections.Generic;
using System.Threading;
using IronLua;

namespace Sample
{
    public class Point
    {
        public Point()
            : this(0 ,0)
        {

        }

        public Point(double _x, double _y)
        {
            x = _x;
            y = _y;
        }

        public double x, y;

        public override string ToString()
        {
            return string.Format("({0}, {1})", x, y);
        }

        public void ThrowException()
        {
            throw new Exception("Test exception");
        }
    }

    public class EventTest
    {     
        public void Trigger()
        {
            if (Tick != null)
                Tick(null);
        }

        public event Func<object,object> Tick = null;
    }

    class Program
    {
        static void Main(string[] args)
        {
            var engine = Lua.CreateEngine();

            var context = engine.GetLuaContext();
            context.SetGlobalVariable("a", 10);
            context.SetGlobalConstant("b", 10);

            var scope = engine.CreateScope();

            WriteLine("Begining Execution", ConsoleColor.Green);

            string code = 
                @"
print('Testing Basic Scope/Context Variable Access')
a = 20
assert(a == 10)
b = 12
assert(b == 10)

print('Testing CLR import and static calls')
c=clr.import('System.Math')
d=clr.call(c,'Pow',10,2)
power=clr.method(c,'Pow')
assert(d == 100)
assert(power(2,4)==16)

print('Testing CLR constructors and instance calls')
point=clr.import('Sample.Point,Sample')
p1=point(10,2)
assert(clr.getvalue(p1,'x') == 10)
assert(clr.getvalue(p1,'y') == 2)
p2=point()
clr.setvalue(p2,'x',5)
assert(clr.getvalue(p2,'x') == 5)
clr.setvalue(p2,'y',10)
assert(clr.getvalue(p2,'y') == 10)
p1type=type(p1)
assert(p1type == 'Sample.Point')

print('Testing CLR events')
eventtest=clr.import('Sample.EventTest,Sample')
eti=eventtest()
handler =   function (e)
                print('Event Received') 
            end
clr.subscribe(eti,'Tick',handler)
clr.call(eti,'Trigger')

print('Testing CLR Syntax Sugar')
eti.Trigger()
assert(p1.x == clr.getvalue(p1,'x'))
p1.x=12
assert(p1.x == 12)

print('All tests passed')

print('CLR API:')
print('clr namespace (clr.*)')
for k in pairs(clr) do
    print('    '..k)
end

f1 = function () p1.ThrowException() end
f2 = function () f1() end

f2()
";

            try
            {
                engine.Execute(code, scope);
            }
            catch (LuaRuntimeException ex)
            {
                var line = ex.GetCurrentCode(code);
                WriteLine("Exception", ConsoleColor.Red);
                Console.WriteLine(ex.Message);
                Console.WriteLine(line);
                line = ex.GetStackTrace(code);
                WriteLine("Stack Trace", ConsoleColor.Red);
                Console.WriteLine(line);
            }


            Console.WriteLine();

            WriteLine("Final Values:", ConsoleColor.Red);
            foreach (var entry in scope.GetVariableNames())
                Console.WriteLine("\t" + entry + ": " + context.FormatObject(scope.GetVariable(entry)));

            dynamic handler = scope.GetVariable("handler");
            handler("Test");

            if (Debugger.IsAttached)
            {
                // Pause for debugger to see console output
                Console.WriteLine();
                Console.Write("Press ENTER to continue..");
                Console.ReadLine();
            }
        }

        static void WriteLine(string text, ConsoleColor color)
        {
            var temp = Console.ForegroundColor;
            Console.ForegroundColor = color;
            Console.WriteLine(text);
            Console.ForegroundColor = temp;
        }

    }
}
