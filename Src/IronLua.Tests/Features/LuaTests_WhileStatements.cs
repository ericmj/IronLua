using System;
using System.Linq;
using System.Text;
using IronLua.Hosting;
using Microsoft.Scripting.Hosting;
using NUnit.Framework;

namespace IronLua.Tests.Features
{
    [TestFixture]
    public class TestWhileStatements
    {
        // ReSharper disable InconsistentNaming

        ScriptEngine engine;

        [OneTimeSetUp]
        public void PrepareEngine()
        {
            engine = Lua.CreateEngine();
        }

        [Test]

        public void TestWhileStmt_10loops_LocalVariable()
        {
            string code = @"
local n = 1
while n < 10 do
  print(n)
  n = n + 1
end";
            string output, error;
            engine.ExecuteTestCode(code, out output, out error);

            var expect = Enumerable.Range(1, 9).Aggregate(new StringBuilder(),
                (sb, i) => sb.AppendFormat("{0}", i).AppendLine()).ToString();

            Assert.That(output, Is.EqualTo(expect));
            Assert.That(error, Is.Empty);
        }

        [Test]
        public void TestWhileStmt_10loops_GlobalVariable()
        {
            string code = @"
n = 1
while n < 10 do
  print(n)
  n = n + 1
end";
            string output, error;
            engine.ExecuteTestCode(code, out output, out error);

            var expect = Enumerable.Range(1, 9).Aggregate(new StringBuilder(),
                (sb, i) => sb.AppendFormat("{0}", i).AppendLine()).ToString();

            Assert.That(output, Is.EqualTo(expect));
            Assert.That(error, Is.Empty);
        }

        [Test]
        public void TestWhileStmt_false()
        {
            string code = @"
while false do
  print 'inside loop'
end";
            string output, error;
            engine.ExecuteTestCode(code, out output, out error);

            Assert.That(output, Is.Empty);
            Assert.That(error, Is.Empty);
        }

        [Test]
        public void TestWhileStmt_nil()
        {
            string code = @"
while nil do
  print 'inside loop'
end";
            string output, error;
            engine.ExecuteTestCode(code, out output, out error);

            Assert.That(output, Is.Empty);
            Assert.That(error, Is.Empty);
        }

        [Test]
        public void TestWhileStmt_true_break()
        {
            string code = @"
while true do
  print 'inside loop'
  break
end";
            string output, error;
            engine.ExecuteTestCode(code, out output, out error);

            Assert.That(output, Is.EqualTo("inside loop" + Environment.NewLine));
            Assert.That(error, Is.Empty);
        }

        [Test]
        public void TestWhileStmt_EmptyTable_break()
        {
            string code = @"
while {} do
  print 'inside loop'
  break
end";
            string output, error;
            engine.ExecuteTestCode(code, out output, out error);

            Assert.That(output, Is.EqualTo("inside loop" + Environment.NewLine));
            Assert.That(error, Is.Empty);
        }

        [Test]
        public void TestWhileStmt_VariableIsEmptyTable_break()
        {
            string code = @"
x = {}
while x do
  print 'inside loop'
  break
end";
            string output, error;
            engine.ExecuteTestCode(code, out output, out error);

            Assert.That(output, Is.EqualTo("inside loop" + Environment.NewLine));
            Assert.That(error, Is.Empty);
        }

        [Test]
        public void TestWhileStmt_VariableIsFunction_break()
        {
            string code = @"
x = function() end
while x do
  print 'inside loop'
  break
end";
            string output, error;
            engine.ExecuteTestCode(code, out output, out error);

            Assert.That(output, Is.EqualTo("inside loop" + Environment.NewLine));
            Assert.That(error, Is.Empty);
        }

        [Test]
        public void TestWhileStmt_CallFunction_break()
        {
            string code = @"
f = function() return true end
while f() do
  print 'inside loop'
  break
end";
            string output, error;
            engine.ExecuteTestCode(code, out output, out error);

            Assert.That(output, Is.EqualTo("inside loop" + Environment.NewLine));
            Assert.That(error, Is.Empty);
        }
    }
}