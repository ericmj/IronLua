using System.IO;
using System.Linq;
using IronLua.Hosting;
using NUnit.Framework;

namespace IronLua.Tests
{
    [TestFixture]
    public class ScriptTests
    {
        string ScriptPath = TestUtils.GetTestPath(@"IronLua.Tests\Scripts");

        [Test]
        [ExpectedException(typeof(LuaRuntimeException), ExpectedMessage = "Assertion failed")]
        public void ExecuteAssertFalse()
        {
            Lua.CreateEngine().Execute("assert(false)");
        }
        [Datapoints]
        public string[] ScriptFiles
        {
            get
            {
                return Directory.EnumerateFiles(ScriptPath, "*.lua")
                                .Select(Path.GetFileName)
                                .Where(f => !Path.GetFileNameWithoutExtension(f).EndsWith("XXX"))
                                .ToArray();
            }
        }

        [Theory]
        public void RunLuaScript(string script)
        {
            Lua.CreateEngine().ExecuteFile(Path.Combine(ScriptPath, script));
        }
    }
}