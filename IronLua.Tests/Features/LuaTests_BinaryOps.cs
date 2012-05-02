using System;
using IronLua.Hosting;
using IronLua.Runtime;
using Microsoft.Scripting.Hosting;
using NUnit.Framework;

namespace IronLua.Tests.Features
{
    // ReSharper disable InconsistentNaming
    // ReSharper disable ConvertToConstant.Local

    [TestFixture]
    public class LuaTests_BinaryOpsAndOr
    {
        ScriptEngine engine;

        [TestFixtureSetUp]
        public void PrepareEngine()
        {
            engine = Lua.CreateEngine();
        }

        public object Run(string code)
        {
            return engine.Execute(code);
        }

        [Test]
        public void TestAssign_AndOprNil()
        {
            Assert.That(Run(@"return nil and nil"), Is.Null);

            Assert.That(Run(@"return nil and false"), Is.Null);
            Assert.That(Run(@"return nil and true"), Is.Null);

            Assert.That(Run(@"return nil and 42"), Is.Null);

            Assert.That(Run(@"return nil and 'a'"), Is.Null);
            
            Assert.That(Run(@"return nil and {}"), Is.Null);
            
            Assert.That(Run(@"return nil and function() end"), Is.Null);

            //Assert.That(Run(@"return false and assert(false)"), Is.Null, "Short-circuit failure");
        }

        [Test]
        public void TestAssign_OrOprNil()
        {
            Assert.That(Run(@"return nil or nil"), Is.Null);

            Assert.That(Run(@"return nil or false"), Is.TypeOf<bool>() & Is.False);
            Assert.That(Run(@"return nil or true"), Is.TypeOf<bool>() & Is.True);

            Assert.That(Run(@"return nil or 10"), Is.EqualTo(10.0));

            Assert.That(Run(@"return nil or 'a'"), Is.EqualTo("a"));

            Assert.That(Run(@"return nil or {}"), Is.TypeOf<LuaTable>());

            Assert.That(Run(@"return nil or function() end"), Is.TypeOf<Func<Object>>());
        }

        [Test]
        public void TestAssign_AndOprFalse()
        {
            Assert.That(Run(@"return false and nil"), Is.False);

            Assert.That(Run(@"return false and false"), Is.TypeOf<bool>() & Is.False);
            Assert.That(Run(@"return false and true"), Is.TypeOf<bool>() & Is.False);

            Assert.That(Run(@"return false and 42"), Is.False);

            Assert.That(Run(@"return false and 'a'"), Is.False);

            Assert.That(Run(@"return false and {}"), Is.False);

            Assert.That(Run(@"return false and function() end"), Is.False);

            //Assert.That(Run(@"return false and assert(false)"), Is.False, "Short-circuit failure");
        }

        [Test]
        public void TestAssign_OrOprFalse()
        {
            Assert.That(Run(@"return false or nil"), Is.Null);

            Assert.That(Run(@"return false or false"), Is.TypeOf<bool>() & Is.False);
            Assert.That(Run(@"return false or true"), Is.TypeOf<bool>() & Is.True);

            Assert.That(Run(@"return false or 10"), Is.EqualTo(10.0));

            Assert.That(Run(@"return false or 'a'"), Is.EqualTo("a"));

            Assert.That(Run(@"return false or {}"), Is.TypeOf<LuaTable>());

            Assert.That(Run(@"return false or function() end"), Is.TypeOf<Func<Object>>());
        }

        [Test]
        public void TestAssign_AndOprTrue()
        {
            Assert.That(Run(@"return true and nil"), Is.Null);

            Assert.That(Run(@"return true and false"), Is.TypeOf<bool>() & Is.False);
            Assert.That(Run(@"return true and true"), Is.TypeOf<bool>() & Is.True);

            Assert.That(Run(@"return true and 42"), Is.EqualTo(42.0));

            Assert.That(Run(@"return true and 'a'"), Is.EqualTo("a"));

            Assert.That(Run(@"return true and {}"), Is.TypeOf<LuaTable>());

            Assert.That(Run(@"return true and function() end"), Is.TypeOf<Func<Object>>());
        }

        [Test]
        public void TestAssign_OrOprTrue()
        {
            Assert.That(Run(@"return true or nil"), Is.True);

            Assert.That(Run(@"return true or false"), Is.True);
            Assert.That(Run(@"return true or true"), Is.True);

            Assert.That(Run(@"return true or 42"), Is.True);

            Assert.That(Run(@"return true or 'a'"), Is.True);

            Assert.That(Run(@"return true or {}"), Is.True);

            Assert.That(Run(@"return true or function() end"), Is.True);

            //Assert.That(Run(@"return true or assert(false)"), Is.True, "Short-circuit failure");
        }

        [Test]
        public void TestBinary_AndOprNumber()
        {
            Assert.That(Run(@"return 10 and nil"), Is.Null);

            Assert.That(Run(@"return 10 and false"), Is.False);
            Assert.That(Run(@"return 10 and true"), Is.True);

            Assert.That(Run(@"return 10 and 42"), Is.EqualTo(42.0));

            Assert.That(Run(@"return 10 and 'a'"), Is.EqualTo("a"));

            Assert.That(Run(@"return 10 and {}"), Is.TypeOf<LuaTable>());

            Assert.That(Run(@"return 10 and function() end"), Is.TypeOf<Func<Object>>());
        }

        [Test]
        public void TestBinary_OrOprNumber()
        {
            Assert.That(Run(@"return 10 or nil"), Is.EqualTo(10.0));

            Assert.That(Run(@"return 10 or false"), Is.EqualTo(10.0));
            Assert.That(Run(@"return 10 or true"), Is.EqualTo(10.0));

            Assert.That(Run(@"return 10 or 20"), Is.EqualTo(10.0));

            Assert.That(Run(@"return 10 or 'a'"), Is.EqualTo(10.0));

            Assert.That(Run(@"return 10 or {}"), Is.EqualTo(10.0));

            Assert.That(Run(@"return 10 or function() end"), Is.EqualTo(10.0));

            //Assert.That(Run(@"return 10 or assert(false)"), Is.EqualTo(10.0), "Short-circuit failure");
        }

        [Test]
        public void TestBinary_AnddOprNumberZero()
        {
            Assert.That(Run(@"return 0 and nil"), Is.Null);

            Assert.That(Run(@"return 0 and false"), Is.False);
            Assert.That(Run(@"return 0 and true"), Is.True);

            Assert.That(Run(@"return 0 and 42"), Is.EqualTo(42.0));

            Assert.That(Run(@"return 0 and 'a'"), Is.EqualTo("a"));

            Assert.That(Run(@"return 0 and {}"), Is.TypeOf<LuaTable>());

            Assert.That(Run(@"return 0 and function() end"), Is.TypeOf<Func<Object>>());
        }

        [Test]
        public void TestBinary_OrOprNumberZero()
        {
            Assert.That(Run(@"return 0 or nil"), Is.EqualTo(0.0));

            Assert.That(Run(@"return 0 or false"), Is.EqualTo(0.0));
            Assert.That(Run(@"return 0 or true"), Is.EqualTo(0.0));

            Assert.That(Run(@"return 0 or 20"), Is.EqualTo(0.0));

            Assert.That(Run(@"return 0 or 'a'"), Is.EqualTo(0.0));

            Assert.That(Run(@"return 0 or {}"), Is.EqualTo(0.0));

            Assert.That(Run(@"return 0 or function() end"), Is.EqualTo(0.0));

            //Assert.That(Run(@"return 0 or assert(false)"), Is.EqualTo(0.0), "Short-circuit failure");
        }

        [Test]
        public void TestBinary_AndOprString()
        {
            Assert.That(Run(@"return 'a' and nil"), Is.Null);

            Assert.That(Run(@"return 'a' and false"), Is.False);
            Assert.That(Run(@"return 'a' and true"), Is.True);

            Assert.That(Run(@"return 'a' and 42"), Is.EqualTo(42.0));

            Assert.That(Run(@"return 'a' and 'b'"), Is.EqualTo("b"));

            Assert.That(Run(@"return 'a' and {}"), Is.TypeOf<LuaTable>());

            Assert.That(Run(@"return 'a' and function() end"), Is.TypeOf<Func<Object>>());
        }

        [Test]
        public void TestBinary_OrOprString()
        {
            Assert.That(Run(@"return 'a' or nil"), Is.EqualTo("a"));

            Assert.That(Run(@"return 'a' or false"), Is.EqualTo("a"));
            Assert.That(Run(@"return 'a' or true"), Is.EqualTo("a"));

            Assert.That(Run(@"return 'a' or 42"), Is.EqualTo("a"));

            Assert.That(Run(@"return 'a' or 'b'"), Is.EqualTo("a"));

            Assert.That(Run(@"return 'a' or {}"), Is.EqualTo("a"));

            Assert.That(Run(@"return 'a' or function() end"), Is.EqualTo("a"));

            //Assert.That(Run(@"return 'a' or assert(false)"), Is.EqualTo("a"), "Short circuit failure");
        }

        [Test]
        public void TestBinary_AndOprStringEmpty()
        {
            Assert.That(Run(@"return '' and nil"), Is.Null);

            Assert.That(Run(@"return '' and false"), Is.False);
            Assert.That(Run(@"return '' and true"), Is.True);

            Assert.That(Run(@"return '' and 42"), Is.EqualTo(42.0));

            Assert.That(Run(@"return '' and 'x'"), Is.EqualTo("x"));

            Assert.That(Run(@"return '' and {}"), Is.TypeOf<LuaTable>());

            Assert.That(Run(@"return '' and function() end"), Is.TypeOf<Func<Object>>());
        }

        [Test]
        public void TestBinary_OrOprStringEmpty()
        {
            Assert.That(Run(@"return '' or nil"), Is.EqualTo(String.Empty));

            Assert.That(Run(@"return '' or false"), Is.EqualTo(String.Empty));
            Assert.That(Run(@"return '' or true"), Is.EqualTo(String.Empty));

            Assert.That(Run(@"return '' or 42"), Is.EqualTo(String.Empty));

            Assert.That(Run(@"return '' or 'b'"), Is.EqualTo(String.Empty));

            Assert.That(Run(@"return '' or {}"), Is.EqualTo(String.Empty));

            Assert.That(Run(@"return '' or function() end"), Is.EqualTo(String.Empty));

            //Assert.That(Run(@"return '' or assert(false)"), Is.EqualTo(String.Empty), "Short-circuit failure");
        }

        [Test, Ignore("Tables not working yet")]
        public void TestBinary_AndOprTable()
        {
            Assert.That(Run(@"return {} and nil"), Is.Null);

            Assert.That(Run(@"return {} and false"), Is.False);
            Assert.That(Run(@"return {} and true"), Is.True);

            Assert.That(Run(@"return {} and 42"), Is.EqualTo(42.0));

            Assert.That(Run(@"return {} and 'x'"), Is.EqualTo("x"));

            Assert.That(Run(@"return {1} and {2}"), Is.TypeOf<LuaTable>());

            Assert.That(Run(@"function f() end; return {} and f"), Is.TypeOf<Func<Object>>());
        }

        [Test, Ignore("Tables not working yet")]
        public void TestBinary_OrOprTable()
        {
            Assert.That(Run(@"return {} or nil"), Is.TypeOf<LuaTable>());

            Assert.That(Run(@"return {} or false"), Is.TypeOf<LuaTable>());
            Assert.That(Run(@"return {} or true"), Is.TypeOf<LuaTable>());

            Assert.That(Run(@"return {} or 42"), Is.TypeOf<LuaTable>());

            Assert.That(Run(@"return {} or 'b'"), Is.TypeOf<LuaTable>());

            Assert.That(Run(@"return {} or {}"), Is.TypeOf<LuaTable>());

            Assert.That(Run(@"function f() end; return {} or f"), Is.TypeOf<LuaTable>());

            Assert.That(Run(@"return {} or assert(false)"), Is.TypeOf<LuaTable>(), "Short-circuit failure");
        }

        [Test]
        public void TestBinary_AndOprFunction()
        {
            Assert.That(Run(@"return function() end and nil"), Is.Null);

            Assert.That(Run(@"return function() end and false"), Is.False);
            Assert.That(Run(@"return function() end and true"), Is.True);

            Assert.That(Run(@"return function() end and 42"), Is.EqualTo(42.0));

            Assert.That(Run(@"return function() end and 'x'"), Is.EqualTo("x"));

            Assert.That(Run(@"return function() end and {}"), Is.TypeOf<LuaTable>());

            var func = Run(@"return function() return 1 end and function() return 2 end");
            Assert.That(func, Is.TypeOf<Func<Object>>());
            Assert.That(((Func<Object>)func)(), Is.EqualTo(2.0));
        }

        [Test]
        public void TestBinary_OrOprFunction()
        {
            Assert.That(Run(@"return function() end or nil"), Is.TypeOf<Func<Object>>());

            Assert.That(Run(@"return function() end or false"), Is.TypeOf<Func<Object>>());
            Assert.That(Run(@"return function() end or true"), Is.TypeOf<Func<Object>>());

            Assert.That(Run(@"return function() end or 42"), Is.TypeOf<Func<Object>>());

            Assert.That(Run(@"return function() end or 'x'"), Is.TypeOf<Func<Object>>());

            Assert.That(Run(@"return function() end or {}"), Is.TypeOf<Func<Object>>());

            var func = Run(@"return function() return 1 end or function() return 2 end");
            Assert.That(func, Is.TypeOf<Func<Object>>());
            Assert.That(((Func<Object>)func)(), Is.EqualTo(1.0));
        }

        [Test, Ignore("Function variables not working yet")]
        public void TestBinary_AndOprFunctionVariable()
        {
            Assert.That(Run(@"function f() end; return f and nil"), Is.Null);

            Assert.That(Run(@"function f() end; return f and false"), Is.False);
            Assert.That(Run(@"function f() end; return f and true"), Is.True);

            Assert.That(Run(@"function f() end; return f and 42"), Is.EqualTo(42.0));

            Assert.That(Run(@"function f() end; return f and 'x'"), Is.EqualTo("x"));

            Assert.That(Run(@"function f() end; return f and {}"), Is.TypeOf<LuaTable>());

            var func = Run(@"function f() return 1 end; function g() return 2 end; return f and g");
            Assert.That(func, Is.TypeOf<Func<Object>>());
            Assert.That(((Func<Object>)func)(), Is.EqualTo(2));
        }

        [Test, Ignore("Functions variables not working yet")]
        public void TestBinary_OrOprFunctionVariable()
        {
            Assert.That(Run(@"function f() end; return f or nil"), Is.TypeOf<Func<Object>>());

            Assert.That(Run(@"function f() end; return f or false"), Is.TypeOf<Func<Object>>());
            Assert.That(Run(@"function f() end; return f or true"), Is.TypeOf<Func<Object>>());

            Assert.That(Run(@"function f() end; return f or 42"), Is.TypeOf<Func<Object>>());

            Assert.That(Run(@"function f() end; return f or 'b'"), Is.TypeOf<Func<Object>>());

            Assert.That(Run(@"function f() end; return f or {}"), Is.TypeOf<Func<Object>>());

            var func = Run(@"function f() return 1 end; function g() return 2 end; return f or g");
            Assert.That(func, Is.TypeOf<Func<Object>>());
            Assert.That(((Func<Object>)func)(), Is.EqualTo(1));

            Assert.That(Run(@"function f() end; return f or assert(false)"), Is.TypeOf<Func<Object>>(), "Short-circuit failure");
        }

        [Test]
        public void TestBinary_NumberStringConvertsion()
        {
            Assert.That(Run(@"return 1 + 2"), Is.EqualTo(3.0));

            Assert.That(Run(@"return '1' .. '2'"), Is.EqualTo("12"));

            Assert.That(Run(@"return 1 .. 2"), Is.EqualTo("12"));

            Assert.That(Run(@"return '1' + '2'"), Is.EqualTo(3.0));
        }

        [Test]
        public void TestBinary_LessThanNil()
        {
            Assert.That(Run("return 'alo' < 'alo1'"), Is.True);
        }
    }
}
