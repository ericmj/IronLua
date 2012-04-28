using System;
using System.Text;
using IronLua.Hosting;
using Microsoft.Scripting;
using Microsoft.Scripting.Hosting;
using NUnit.Framework;

namespace IronLua.Tests.Features
{
    // ReSharper disable InconsistentNaming
    // ReSharper disable ConvertToConstant.Local

    [TestFixture]
    public class LuaTests_Tables
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
            dynamic result = engine.ExecuteTestCode(code, out output, out error);

            Assert.That((object)result, Is.Null);
            Assert.That(output, Is.EqualTo(expect + Environment.NewLine));
            Assert.That(error, Is.Empty);
        }

        public void PerformTableTest(string code)
        {
            var scope = engine.CreateScope();

            string output, error;
            dynamic result = engine.ExecuteTestCode(code, scope, out output, out error);

            Assert.That((object)result, Is.Null);
            Assert.That(output, Is.Empty);
            Assert.That(error, Is.Empty);

            //Assert.That(scope.ContainsVariable("t"), Is.True); // TODO: get scope to work
        }

        [Test]
        public void TestTables_EmptyTableAssignment_local()
        {
            string code = @"local t = {}";

            var scope = engine.CreateScope();

            string output, error;
            dynamic result = engine.ExecuteTestCode(code, scope, out output, out error);

            Assert.That((object)result, Is.Null);
            Assert.That(output, Is.Empty);
            Assert.That(error, Is.Empty);

            Assert.That(scope.ContainsVariable("t"), Is.False);
        }

        [Test]
        public void TestTables_EmptyTableAssignment()
        {
            string code = @"t = {}";

            PerformTableTest(code);
        }

        [Test]
        public void TestTables_SingleEntry()
        {
            string code = @"t = { x = 1 }";

            PerformTableTest(code);
        }

        [Test]
        public void TestTables_SimpleArray()
        {
            string code = @"t = {'yes','no','maybe'}";

            PerformTableTest(code);
        }

        [Test]
        public void TestTables_SimpleArrayIndexed()
        {
            string code = @"t = {[1]='yes', [2]='no', [3]='maybe'}";

            PerformTableTest(code);
        }

        [Test]
        public void TestTables_SparseArray()
        {
            string code = @"t = { [900]=3, [-900]=4 }";

            PerformTableTest(code);
        }

        [Test]
        public void TestTables_HashTable()
        {
            string code = @"t = {x=5, y = 10 }";

            PerformTableTest(code);
        }

        [Test]
        public void TestTables_HashTableMixedWithArray()
        {
            string code = @"t = {x=5, y = 10; 'yes', 'no' }";

            PerformTableTest(code);
        }

        [Test]
        public void TestTables_TableContainingTableAsField()
        {
            string code = @"t = { msg='choice', {'yes','no','maybe'} }";

            PerformTableTest(code);
        }

        [Test]
        public void TestTables_MixedSlotArrayFields()
        {
            string code = @"
t = { 'A', [3]='B', 'C', 'D', 'E' }
for k,v in pairs(t) do
  print(k,v, type(k))
end";
            var expect = new StringBuilder();
            expect.AppendLine("1\tA\tnumber");
            expect.AppendLine("2\tC\tnumber");
            expect.AppendLine("3\tD\tnumber");
            expect.AppendLine("4\tE\tnumber");
            PerformTest(code, expect.ToString());
        }


        [Test]
        [ExpectedException(typeof(SyntaxErrorException), 
            ExpectedMessage = "unexpected symbol near ';'")]
        public void TestTables_ParserMsg1a()
        {
            engine.Execute("t = { ; }");
        }

        [Test]
        [ExpectedException(typeof(SyntaxErrorException),
            ExpectedMessage = "unexpected symbol near ','")]
        public void TestTables_ParserMsg1b()
        {
            engine.Execute("t = { , }");
        }

        [Test]
        [ExpectedException(typeof(SyntaxErrorException), 
            ExpectedMessage = "'}' expected near '2'")]
        public void TestTables_ParserMsg2()
        {
            engine.Execute("t = { 1 2 }");
        }

        [Test]
        [ExpectedException(typeof(SyntaxErrorException),
            ExpectedMessage = "'}' expected (to close '{' at line 1) near '2'")]
        public void TestTables_ParserMsg3()
        {
            engine.Execute("t = { 1\r\n2 }");
        }

        [Test]
        [ExpectedException(typeof(SyntaxErrorException),
            ExpectedMessage = "unexpected symbol near '='")]
        public void TestTables_ParserMsg4a()
        {
            engine.Execute("t = { [ = 2 }");
        }

        [Test]
        [ExpectedException(typeof(SyntaxErrorException),
            ExpectedMessage = "']' expected near '='")]
        public void TestTables_ParserMsg4b()
        {
            engine.Execute("t = { [1 = 2 }");
        }

        [Test]
        [ExpectedException(typeof(SyntaxErrorException),
            ExpectedMessage = "'=' expected near '2'")]
        public void TestTables_ParserMsg4c()
        {
            engine.Execute("t = { [1] 2 }");
        }

        [Test]
        [ExpectedException(typeof(SyntaxErrorException),
            ExpectedMessage = "unexpected symbol near '}'")]
        public void TestTables_ParserMsg4d()
        {
            engine.Execute("t = { [1] = }");
        }

        [Test]
        [ExpectedException(typeof(SyntaxErrorException),
            ExpectedMessage = "unexpected symbol near ','")]
        public void TestTables_ParserMsg4e()
        {
            engine.Execute("t = { [1] = , }");
        }

        [Test]
        [ExpectedException(typeof(SyntaxErrorException),
            ExpectedMessage = "unexpected symbol near '}'")]
        public void TestTables_ParserMsg5b()
        {
            engine.Execute("t = { a = }");
        }
    }
}
