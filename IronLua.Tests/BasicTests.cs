using System;
using IronLua.Hosting;
using IronLua.Runtime;
using NUnit.Framework;

namespace IronLua.Tests
{
    [TestFixture]
    public class BasicTests
    {
        [Test]
        public void ExecuteAssertTrue()
        {
            Lua.CreateEngine().Execute("assert(true)");
        }

        [Test]
        [ExpectedException(typeof(LuaRuntimeException), ExpectedMessage = "Assertion failed")]
        public void ExecuteAssertFalse()
        {
            Lua.CreateEngine().Execute("assert(false)");
        }

        [Test]
        public void ExecuteOnePlusOne()
        {
            Lua.CreateEngine().Execute("a = 1 + 1; assert(a == 2)");
        }

        [Test]
        public void ExecuteResultNil()
        {
            Assert.IsNull(Lua.CreateEngine().Execute("return nil"));
        }

        [Test]
        public void ExecuteResultBoolean()
        {
            var engine = Lua.CreateEngine();

            dynamic b = engine.Execute("return true");
            Assert.That(b, Is.TypeOf<bool>());
            Assert.That(b, Is.EqualTo(true));

            b = engine.Execute("return false");
            Assert.That(b, Is.TypeOf<bool>());
            Assert.That(b, Is.EqualTo(false));
        }

        [Test]
        public void ExecuteResultNumber()
        {
            var x = Lua.CreateEngine().Execute("return 1 + 2");
            Assert.That(x, Is.TypeOf<double>());
            Assert.That(x, Is.EqualTo(3.0));
        }

        [Test]
        public void ExecuteResultString()
        {
            dynamic s = Lua.CreateEngine().Execute("return 'two cows jump over the moon'");

            Assert.That(s, Is.TypeOf<string>());
            Assert.That(s, Is.EqualTo("two cows jump over the moon"));
        }


        [Test]
        public void ExecuteResultTable()
        {
            dynamic t = Lua.CreateEngine().Execute("return { 5, 10, 15, A = 'alpha', ['B'] = 'beta' }");

            Assert.That(t, Is.TypeOf<LuaTable>());
            Assert.That(t.A, Is.EqualTo("alpha"));
            Assert.That(t.B, Is.EqualTo("beta"));

            // broken
            //Assert.That(t[1], Is.EqualTo(5));
            //Assert.That(t[2], Is.EqualTo(10));
            //Assert.That(t[3], Is.EqualTo(15));
            //Assert.That(t["B"], Is.EqualTo("beta"));
        }

        [Test]
        public void ExecuteEmptyTable()
        {
            var t = Lua.CreateEngine().Execute("t = {}; assert(type(t) == 'table'); return t");
            Assert.That(t, Is.TypeOf<LuaTable>());
        }

        [Test]
        public void StrangeNumbers()
        {
            var engine = Lua.CreateEngine();

            Assert.That(engine.Execute("return 0x.8"), Is.EqualTo(0.5));            
        }

        [Test]
        public void ScopeTest01()
        {
            var engine = Lua.CreateEngine();
            var scope = engine.CreateScope();

            scope.SetVariable("a", 42);
            object a = scope.GetVariable("a");
            Assert.That(a, Is.EqualTo(42.0));
        }


        [Test]
        public void ScopeTest02()
        {
            var engine = Lua.CreateEngine();
            var scope = engine.CreateScope();

            engine.Execute("a = 42", scope);
            object a = scope.GetVariable("a");
            Assert.That(a, Is.EqualTo(42));
        }

        [Test]
        public void ScopeTest03()
        {
            var engine = Lua.CreateEngine();
            var scope = engine.CreateScope();

            scope.SetVariable("a", 42);
            //engine.Execute("assert(a == 42)");
            object la = engine.Execute("return a", scope); 
            Assert.That(la, Is.EqualTo(42));
        }
    }
}
