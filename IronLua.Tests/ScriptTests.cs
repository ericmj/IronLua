using System;
using System.IO;
using System.Linq;
using IronLua.Hosting;
using IronLua.Tests.Compiler;
using Microsoft.Scripting;
using Microsoft.Scripting.Hosting;
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
        // See ParserTest class for documentation

        public void ExecuteLuaTestSuite(string testCaseFile)
        {
            ParserTests.AssertSyntaxError(() =>
            {
                Lua.CreateEngine().ExecuteFile(testCaseFile);
            });
        }

        [Test, TestCaseSource(typeof(ParserTests.LuaTestSuiteSource), "Lua52TestCases")]
        public void ExcuteTestOnLua52TestSuite(string luaFile)
        {
            ExecuteLuaTestSuite(luaFile);
        }

        //[Test, TestCaseSource(typeof(ParserTests.LuaTestSuiteSource), "Lua51TestCases")]
        public void ExcuteTestOnLua51TestSuite(string luaFile)
        {
            ExecuteLuaTestSuite(luaFile);
        }
    }
}