using System;
using System.Text;
using IronLua.Hosting;
using Microsoft.Scripting.Hosting;
using NUnit.Framework;

namespace IronLua.Tests.Features
{
    // ReSharper disable InconsistentNaming

    [TestFixture]
    public class LuaTests_Errors
    {
        ScriptEngine engine;

        [TestFixtureSetUp]
        public void PrepareEngine()
        {
            engine = Lua.CreateEngine();
        }

        public void PerformTest(string code, string expect)
        {
            string output, error;
            engine.ExecuteTestCode(code, out output, out error);

            Assert.That(output, Is.EqualTo(expect + Environment.NewLine));
            Assert.That(error, Is.Empty);
        }

        [Test, ExpectedException(ExpectedException = typeof(LuaErrorException), ExpectedMessage = "error message")]
        public void TestErrors_Throw()
        {
            engine.Execute("error('error message')");
        }

        [Test]
        public void TestErrors_PCall()
        {
            dynamic error = engine.Execute("a,x = pcall(error, 'error message'); return x");
            Assert.That((object)error != null);
            Assert.That((string)error == "error message");
        }

        [Test]
        public void TestErrors_XPCall()
        {
            string code = 
@"handler = function() x = 'error message' end
xpcall(function () error('error message2') end, handler)
return x";
            dynamic error = engine.Execute(code);
            Assert.That((object)error != null);
            Assert.That((string)error == "error message");
        }

        [Test]
        public void TestErrors_ChunkLevelCall()
        {
            string code = @"error('error message')";
            try
            {
                engine.Execute(code);
            }
            catch (LuaErrorException ex)
            {
                Assert.That(ex.StackLevel == 1);
                Assert.That(ex.Message == "error message");
                Assert.That(ex.Result == "error message");

                return;
            }

            Assert.Fail();
        }
    }
}
