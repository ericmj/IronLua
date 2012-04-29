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
            ExpectedMessage = "<name> expected near '~'")]
        public void TestLocal_ParserMsg1a()
        {
            engine.Execute("local ~ a = 1");
        }

        [Test]
        [ExpectedException(typeof(SyntaxErrorException),
            ExpectedMessage = "<name> expected near '123'")]
        public void TestLocal_ParserMsg1b()
        {
            engine.Execute("local 123 a = 1");
        }

        [Test]
        [ExpectedException(typeof(SyntaxErrorException),
            ExpectedMessage = "unexpected symbol near '~'")]
        public void TestLocal_ParserMsg2()
        {
            engine.Execute("local a ~ b = 1, 2");
        }

        [Test]
        [ExpectedException(typeof(SyntaxErrorException),
            ExpectedMessage = "'(' expected near '.'")]
        public void TestLocal_ParserMsg3()
        {
            engine.Execute("local function t.f() end");
        }

        [Test]
        [ExpectedException(typeof(SyntaxErrorException),
            ExpectedMessage = "'(' expected near '.'")]
        public void TestNumber_ParserMsg4()
        {
            engine.Execute("local function t.f() end");
        }
    }
}
