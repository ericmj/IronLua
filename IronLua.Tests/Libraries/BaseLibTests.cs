using System;
using IronLua.Hosting;
using IronLua.Runtime;
using Microsoft.Scripting.Hosting;
using NUnit.Framework;

namespace IronLua.Tests.Libraries
{
    [TestFixture]
    public class BaseLibTests
    {
        ScriptEngine engine;

        [TestFixtureSetUp]
        public void PrepareEngine()
        {
            engine = Lua.CreateEngine();
        }

        [Test]
        public void TestVersionString()
        {
            dynamic v = engine.Execute(@"return _VERSION");
            Assert.That(v, Is.EqualTo("Lua 5.1"));
        }

        [Test]
        public void TestPairs1()
        {
            dynamic v = engine.Execute(@"return pairs({a=1,b=2,c=3})");

            Assert.That(v, Is.TypeOf<Varargs>());
            var varg = (Varargs) v;
            Assert.That(varg.Count, Is.EqualTo(3));
            Assert.That(varg[0], Is.TypeOf<Func<LuaTable, object, Varargs>>()); // 'next' function
            Assert.That(varg[1], Is.TypeOf<LuaTable>());                        // table passed to pairs 
            Assert.That(varg[2], Is.Null);                                      // initial index
        }

        [Test]
        public void TestNextInitial()
        {
            dynamic v = engine.Execute(@"return next({a=1,b=2,c=3}, nil)");

            Assert.That(v, Is.TypeOf<Varargs>());
            var varg = (Varargs)v;
            Assert.That(varg.Count, Is.EqualTo(2));
            Assert.That(varg[0], Is.EqualTo("a"));
            Assert.That(varg[1], Is.EqualTo(1.0));

            Assert.That(v.Count, Is.EqualTo(2));
            Assert.That(v[0], Is.EqualTo("a"));
            Assert.That(v[1], Is.EqualTo(1.0));
        }

        [Test]
        public void TestNextMiddle()
        {
            dynamic v = engine.Execute(@"return next({a=1,b=2,c=3}, 'a')");

            Assert.That(v, Is.TypeOf<Varargs>());
            var varg = (Varargs)v;
            Assert.That(varg.Count, Is.EqualTo(2));
            Assert.That(varg[0], Is.EqualTo("b"));
            Assert.That(varg[1], Is.EqualTo(2.0));
        }

        [Test]
        public void TestNextLast()
        {
            dynamic v = engine.Execute(@"return next({a=1,b=2,c=3}, 'c')");
            Assert.That((object)v, Is.Null);
        }

        [Test]
        public void TestNextEmpty()
        {
            dynamic v = engine.Execute(@"return next({}, nil)");
            Assert.That((object)v, Is.Null);

            v = engine.Execute(@"return next({})");
            Assert.That((object)v, Is.Null);
        }

        [Test]
        public void TestForInLoop()
        {
            string code = @"for k, v in pairs({a = 1, b = 2, c = 3}) do print(k,v) end";

            string output, error;
            engine.CaptureOutput(e => e.Execute(code), out output, out error);

            Assert.That(output, Is.EqualTo("a\t1\r\nb\t2\r\nc\t3\r\n"));
        } 

        [Test]
        public void TestForLoop()
        {
            string code = @"for i = 1,3 do print(i) end";

            string output, error;
            engine.CaptureOutput(e => e.Execute(code), out output, out error);

            Assert.That(output, Is.EqualTo("1\r\n2\r\n3\r\n"));
        }
    }
}
