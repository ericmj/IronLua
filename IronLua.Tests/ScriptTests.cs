using System;
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
        [ExpectedException(typeof(LuaRuntimeException), ExpectedMessage = "Assertion failed")]
        public void ExecuteAssertFalse()
        {
            Lua.CreateEngine().Execute("assert(false)");
        }

        const string ScriptPath = @"F:\workspace\DLR\IronLua-github\IronLua.Tests\Scripts";

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

    [TestFixture]    
    public class ScriptTestCases
    {
        //string TestCasePath = @"F:\workspace\DLR\IronLua-github\lua-5.2.0-tests";
        string TestCasePath = @"F:\workspace\DLR\IronLua-github\lua-5.1-tests";

        [Datapoints]
        public string[] TestCaseFiles = new[]
        {
            //"all.lua", 
            //"api.lua",
            //"attrib.lua",
            //"big.lua",
            //"bitwise.lua",
            //"calls.lua",
            //"checktable.lua",
            //"closure.lua",
            //"code.lua",
            //"constructs.lua",
            //"coroutine.lua",
            //"db.lua",
            //"errors.lua",
            //"events.lua",
            //"files.lua",
            //"gc.lua",
            //"goto.lua",
            //"literals.lua",
            //"locals.lua",
            //"main.lua",
            "math.lua",
            //"nextvar.lua",
            //"pm.lua",
            //"sort.lua",
            //"strings.lua",
            //"vararg.lua",
            //"verybig.lua"
        };

        //[Ignore]
        [Theory]
        public void ExecuteLuaTestSuite(string testCaseFile)
        {
            var engine = Lua.CreateEngine();
            Console.WriteLine("Executing {0}", testCaseFile);
            engine.ExecuteFile(Path.Combine(TestCasePath, testCaseFile));            
        }
    }
}