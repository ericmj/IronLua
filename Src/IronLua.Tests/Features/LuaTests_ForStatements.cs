using System;
using System.Linq;
using IronLua.Hosting;
using Microsoft.Scripting.Hosting;
using NUnit.Framework;

namespace IronLua.Tests.Features
{
    // ReSharper disable InconsistentNaming
    // ReSharper disable ConvertToConstant.Local

    [TestFixture]
    public class LuaTests_ForStatements
    {
        readonly string NewLine = Environment.NewLine;

        ScriptEngine engine;

        [OneTimeSetUp]
        public void PrepareEngine()
        {
            engine = Lua.CreateEngine();
        }

        public void PerformTest(string code, string expect)
        {
            string output, error;
            dynamic result = engine.ExecuteTestCode(code, out output, out error);

            Assert.That((object)result, Is.Null);
            Assert.That(output, Is.EqualTo(expect + Environment.NewLine));
            Assert.That(error, Is.Empty);
        }

        [Test]
        public void TestForStmt_1to10()
        {
            string code = @"
for n = 1, 10 do
  print(n)
end";
            var expect = String.Join(NewLine, Enumerable.Range(1, 10));

            PerformTest(code, expect);
        }

        [Test]
        public void TestForStmt_1to10_break()
        {
            string code = @"
for n = 1, 10 do
  print(n)
  break
end";
            PerformTest(code, "1");
        }

        [Test]
        public void TestForStmt_1to10_step2()
        {
            string code = @"
for n = 1, 10, 2 do
  print(n)
end";
            var expect = String.Join(NewLine, Enumerable.Range(1, 10).Where(i => i % 2 == 1));

            PerformTest(code, expect);
        }

        [Test]
        public void TestForStmt_1to10_step2_break()
        {
            string code = @"
for n = 1, 10, 2 do
  print(n)
  break
end";
            PerformTest(code, "1");
        }
    }
}

