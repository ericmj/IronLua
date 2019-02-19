using System;
using System.Linq;
using IronLua.Hosting;
using Microsoft.Scripting.Hosting;
using NUnit.Framework;

namespace IronLua.Tests.Features
{
    // ReSharper disable InconsistentNaming

    [TestFixture]
    public class LuaTests_RepeatStatements
    {
        ScriptEngine engine;

        [OneTimeSetUp]
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

        [Test]
        public void TestRepeat_EmptyLoop()
        {
            engine.Execute(@"repeat until true");
            // should not throw any exceptions
        }

        [Test]
        public void TestRepeat_UntilUseLocalVariableDeclaredInsideLoop()
        {
            engine.Execute(@"repeat local i = 42 until i == 42");
            // should not throw any exceptions
        }

        [Test]
        public void TestRepeat_10loops_GlobalVariable()
        {
            string code = @"
n = 1
repeat
  print(n)
  n = n + 1
until n >= 10";

            var expect = String.Join(Environment.NewLine, Enumerable.Range(1,9));

            PerformTest(code, expect);
        }

        [Test]
        public void TestRepeat_10loops_LocalVariable()
        {
            string code = @"
local n = 1
repeat
  print(n)
  n = n + 1
until n >= 10";

            var expect = String.Join(Environment.NewLine, Enumerable.Range(1, 9));

            PerformTest(code, expect);
        }


        [Test]
        public void TestRepeat_true()
        {
            string code = @"
n = 1
repeat
  print(n)
  n = n + 1
until true";

            var expect = String.Join(Environment.NewLine, Enumerable.Range(1, 1));

            PerformTest(code, expect);
        }

        [Test]
        public void TestRepeat_true_if10break()
        {
            string code = @"
n = 9
repeat
  print(n)
  if n <= 1 then
    break
  end
  n = n - 1
until n <= 0";

            var expect = String.Join(Environment.NewLine, Enumerable.Range(1, 9).Reverse());

            PerformTest(code, expect);
        }
    }
}