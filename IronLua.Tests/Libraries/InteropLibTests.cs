using System;
using IronLua.Hosting;
using IronLua.Runtime;
using Microsoft.Scripting.Hosting;
using NUnit.Framework;

namespace IronLua.Tests.Libraries
{
    [TestFixture]
    public class InteropLibTests
    {
        ScriptEngine engine;

        [TestFixtureSetUp]
        public void PrepareEngine()
        {
            engine = Lua.CreateEngine();
        }
                
        [Test]
        public void TestImport()
        {
            string code = @"clrmath = clr.import('System.Math')
clrpow = clrmath('Pow',10,2)";

            

            var scope = engine.CreateScope();

            engine.Execute(code, scope);

            Assert.NotNull(scope.GetVariable("clrmath"));

            var clrmath = scope.GetVariable("clrmath") as LuaTable;
            var __index = clrmath.GetValue("__index") as Func<object, object, object>;
            var clrPow = __index(clrmath, "Pow") as Func<Varargs, object>;

            var result = clrPow(new Varargs(10.0, 2.0));

            Assert.AreEqual(100, scope.GetVariable("clrpow"));
        }
    }
}
