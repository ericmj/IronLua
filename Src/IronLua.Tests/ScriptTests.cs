using System.IO;
using System.Linq;
using IronLua.Hosting;
using NUnit.Framework;

namespace IronLua.Tests
{
    [TestFixture]
    public class ScriptTests
    {
        [Test]
        public void ExecuteAssertFalse()
        {
            Assert.Throws<LuaRuntimeException>(() =>
                {
                    Lua.CreateEngine().Execute("assert(false)");
                })
                .WithMessage("Assertion failed");

        }

        [Test, TestCaseSource(typeof(ScriptSources), nameof(ScriptSources.GetTestCases))]
        public void RunLuaScripts(string luaFile)
        {
            Lua.CreateEngine().ExecuteFile(luaFile);
        }

        public static class ScriptSources
        {
            static string ScriptPath = TestUtils.GetTestPath(@"IronLua.Tests\Scripts");

            public static TestCaseData[] GetTestCases()
            {
                var query = from f in Directory.EnumerateFiles(ScriptPath, "*.lua")
                            where !Path.GetFileNameWithoutExtension(f).EndsWith("XXX")
                            select new TestCaseData(f).SetName(Path.GetFileName(f));
                return query.ToArray();
            }
        }
    }
}