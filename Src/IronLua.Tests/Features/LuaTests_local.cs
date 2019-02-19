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

        [OneTimeSetUp]
        public void PrepareEngine()
        {
            engine = Lua.CreateEngine();
        }

        [Test]
        public void TestLocal_ParserMsg1a()
        {
            Assert.Throws<SyntaxErrorException>(() =>
                {
                    engine.Execute("local ~ a = 1");
                })
                .WithMessage("<name> expected near '~' (line 1, column 7)");
        }

        [Test]
        public void TestLocal_ParserMsg1b()
        {
            Assert.Throws<SyntaxErrorException>(() =>
                {
                    engine.Execute("local 123 a = 1");
                })
                .WithMessage("<name> expected near '123' (line 1, column 7)");
        }

        [Test]
        public void TestLocal_ParserMsg2()
        {
            Assert.Throws<SyntaxErrorException>(() =>
                {
                    engine.Execute("local a ~ b = 1, 2");
                })
                .WithMessage("unexpected symbol near '~' (line 1, column 9)");
        }

        [Test]
        public void TestLocal_ParserMsg3()
        {
            Assert.Throws<SyntaxErrorException>(() =>
                {
                    engine.Execute("local function t.f() end");
                })
                .WithMessage("'(' expected near '.' (line 1, column 17)");
        }

        [Test]
        public void TestLocal_ParserMsg4()
        {
            Assert.Throws<SyntaxErrorException>(() =>
                {
                    engine.Execute("local function t.f() end");
                })
                .WithMessage("'(' expected near '.' (line 1, column 17)");
        }

        [Test]
        public void TestLocal_SingleVariableWithNoAssignment()
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
