using System.Collections.Generic;
using IronLua.Hosting;
using NUnit.Framework;

namespace IronLua.Tests.Compiler
{
    [TestFixture]
    public class ExecutionTests
    {
        // See ParserTest class for documentation

        public void ExecuteLuaTestSuite(string testCaseFile, bool useLua52)
        {
            var options = new Dictionary<string, object>()
            { 
                { "UseLua52Features", useLua52 }, // TODO: need to make use of these options inside CreateEngine
            };
            var engine = Lua.CreateEngine(options); 

            TestUtils.AssertSyntaxError(() =>
            {
                engine.ExecuteFile(testCaseFile);
            });
        }

        [Test, TestCaseSource(typeof(ParserTests.LuaTestSuiteSource), "Lua52TestCases")]
        public void ExcuteTestOnLua52TestSuite(string luaFile)
        {
            ExecuteLuaTestSuite(luaFile, useLua52:true);
        }

        //[Test, TestCaseSource(typeof(ParserTests.LuaTestSuiteSource), "Lua51TestCases")]
        public void ExcuteTestOnLua51TestSuite(string luaFile)
        {
            ExecuteLuaTestSuite(luaFile, useLua52:false);
        }
    }
}
