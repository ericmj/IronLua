using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using IronLua.Hosting;
using Microsoft.Scripting;
using Microsoft.Scripting.Hosting;
using NUnit.Framework;

namespace IronLua.Tests.Features
{
    // ReSharper disable InconsistentNaming

    [TestFixture]
    public class LuaTests_local
    {
        ScriptEngine engine;

        [TestFixtureSetUp]
        public void PrepareEngine()
        {
            engine = Lua.CreateEngine();
        }

        [Test]
        [ExpectedException(typeof(SyntaxErrorException),
            ExpectedMessage = "<name> expected near '~' (line 1, column 7)")]
        public void TestLocal_ParserMsg1a()
        {
            engine.Execute("local ~ a = 1");
        }

        [Test]
        [ExpectedException(typeof(SyntaxErrorException),
            ExpectedMessage = "<name> expected near '123' (line 1, column 7)")]
        public void TestLocal_ParserMsg1b()
        {
            engine.Execute("local 123 a = 1");
        }

        [Test]
        [ExpectedException(typeof(SyntaxErrorException),
            ExpectedMessage = "unexpected symbol near '~' (line 1, column 9)")]
        public void TestLocal_ParserMsg2()
        {
            engine.Execute("local a ~ b = 1, 2");
        }

        [Test]
        [ExpectedException(typeof(SyntaxErrorException),
            ExpectedMessage = "'(' expected near '.' (line 1, column 17)")]
        public void TestLocal_ParserMsg3()
        {
            engine.Execute("local function t.f() end");
        }

        [Test]
        [ExpectedException(typeof(SyntaxErrorException),
            ExpectedMessage = "'(' expected near '.' (line 1, column 17)")]
        public void TestLocal_ParserMsg4()
        {
            engine.Execute("local function t.f() end");
        }

        [Test]
        public void TestLocal_SingleVariableWithNoAsighment()
        {
            engine.Execute("local a");
            // no exception should be thrown.
        }

        [Test]
        public void TestLocal_TwoVariablesWithNoAsighment()
        {
            engine.Execute("local a, b");
            // no exception should be thrown.
        }
    }
}
