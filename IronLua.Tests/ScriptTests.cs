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