using System;
using System.Text;
using IronLua.Hosting;
using IronLua.Runtime;
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

        object Run(string code)
        {
            return engine.Execute(code);
        }

        public void PerformTest(string code, string expect)
        {
            string output, error;
            dynamic result = engine.ExecuteTestCode(code, out output, out error);

            Assert.That((object)result, Is.Null);
            Assert.That(output, Is.EqualTo(expect + Environment.NewLine));
            Assert.That(error, Is.Empty);
        }

        public object PerformTableTest(string code)
        {
            var scope = engine.CreateScope();

            string output, error;
            dynamic result = engine.ExecuteTestCode(code, scope, out output, out error);

            Assert.That((object)result, Is.Null);
            Assert.That(output, Is.Empty);
            Assert.That(error, Is.Empty);

            Assert.That(scope.ContainsVariable("t"), Is.True);

            return scope.GetVariable("t");
        }

        [Test]
        public void TestTables_EmptyTableAssignment_local()
        {
            string code = @"local t = {}";

            var scope = engine.CreateScope();

            string output, error;
            dynamic result = engine.CaptureOutput(e => e.Execute(code, scope), out output, out error);

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

            dynamic table = PerformTableTest(code);
            Assert.That(table.x == 5, Is.True);
            Assert.That(table["y"] == 10, Is.True);
            Assert.That(table[1.0] == "yes", Is.True);
            Assert.That(table[2.0] == "no", Is.True);
        }

        [Test]
        public void TestTables_TableContainingTableAsField()
        {
            string code = @"t = { msg='choice', {'yes','no','maybe'} }";

            dynamic table = PerformTableTest(code);
            Assert.That(table.msg, Is.EqualTo("choice"));

            dynamic table2 = table[1.0];
            Assert.That((object)table2 != null, Is.True, "Sub-table was nil");

            Assert.That(table2[1.0] == "yes", Is.True);
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
            expect.AppendLine("3\tD\tnumber");
            expect.AppendLine("2\tC\tnumber");
            expect.Append    ("4\tE\tnumber");
            PerformTest(code, expect.ToString());
        }

        [Test]
        public void TestTables_IndexInstantiation()
        {
            string code = 
@"t = {}
t.x = 5
t['y'] = 10
t['z'] = t.y";

            dynamic table = PerformTableTest(code);

            Assert.That(table.x == 5, Is.True);
            Assert.That(table.y == 10, Is.True);
            Assert.That(table.z == 10, Is.True);
        }

        #region Syntax Exception tests

        [Test]
        [ExpectedException(typeof(SyntaxErrorException),
            ExpectedMessage = "unexpected symbol near ';' (line 1, column 7)")]
        public void TestTables_ParserMsg1a()
        {
            engine.Execute("t = { ; }");
        }

        [Test]
        [ExpectedException(typeof(SyntaxErrorException),
            ExpectedMessage = "unexpected symbol near ',' (line 1, column 7)")]
        public void TestTables_ParserMsg1b()
        {
            engine.Execute("t = { , }");
        }

        [Test]
        [ExpectedException(typeof(SyntaxErrorException), 
            ExpectedMessage = "'}' expected near '2' (line 1, column 9)")]
        public void TestTables_ParserMsg2()
        {
            engine.Execute("t = { 1 2 }");
        }

        [Test]
        [ExpectedException(typeof(SyntaxErrorException),
            ExpectedMessage = "'}' expected (to close '{' at line 1) near '2' (line 2, column 1)")]
        public void TestTables_ParserMsg3()
        {
            engine.Execute("t = { 1\r\n2 }");
        }

        [Test]
        [ExpectedException(typeof(SyntaxErrorException),
            ExpectedMessage = "unexpected symbol near '=' (line 1, column 9)")]
        public void TestTables_ParserMsg4a()
        {
            engine.Execute("t = { [ = 2 }");
        }

        [Test]
        [ExpectedException(typeof(SyntaxErrorException),
            ExpectedMessage = "']' expected near '=' (line 1, column 10)")]
        public void TestTables_ParserMsg4b()
        {
            engine.Execute("t = { [1 = 2 }");
        }

        [Test]
        [ExpectedException(typeof(SyntaxErrorException),
            ExpectedMessage = "'=' expected near '2' (line 1, column 11)")]
        public void TestTables_ParserMsg4c()
        {
            engine.Execute("t = { [1] 2 }");
        }

        [Test]
        [ExpectedException(typeof(SyntaxErrorException),
            ExpectedMessage = "unexpected symbol near '}' (line 1, column 13)")]
        public void TestTables_ParserMsg4d()
        {
            engine.Execute("t = { [1] = }");
        }

        [Test]
        [ExpectedException(typeof(SyntaxErrorException),
            ExpectedMessage = "unexpected symbol near ',' (line 1, column 13)")]
        public void TestTables_ParserMsg4e()
        {
            engine.Execute("t = { [1] = , }");
        }

        [Test]
        [ExpectedException(typeof(SyntaxErrorException),
            ExpectedMessage = "unexpected symbol near '}' (line 1, column 11)")]
        public void TestTables_ParserMsg5b()
        {
            engine.Execute("t = { a = }");
        }

        [Test]
        [ExpectedException(typeof(SyntaxErrorException),
            ExpectedMessage = "'}' expected near '=' (line 1, column 11)")]
        public void TestTables_ParserMsg6()
        {
            engine.Execute("t = { f() = 3, 2, 1 }");
        }

        #endregion

        #region Table construction tests
        
        [Test]
        public void TestTables_Empty()
        {
            dynamic t = engine.Execute(@"return {}");
            Assert.That(t, Is.TypeOf<LuaTable>());
            var lt = (LuaTable)t;
            Assert.That(lt.Length(), Is.EqualTo(0));
        }

        [Test]
        public void TestTables_SimpleArray2()
        {
            dynamic t = engine.Execute(@"return { 1, 2, 3 }");
            Assert.That(t, Is.TypeOf<LuaTable>());
            var lt = (LuaTable) t;
            Assert.That(lt.Length(), Is.EqualTo(3));
            Assert.That(lt.GetValue(1.0), Is.EqualTo(1.0));
            Assert.That(lt.GetValue(2.0), Is.EqualTo(2.0));
            Assert.That(lt.GetValue(3.0), Is.EqualTo(3.0));
        }

        [Test]
        public void TestTables_SimpleArray3()
        {
            dynamic t = engine.Execute(@"return { 'a', 'b', 'c' }");
            Assert.That(t, Is.TypeOf<LuaTable>());
            var lt = (LuaTable)t;
            Assert.That(lt.Length(), Is.EqualTo(3));
            Assert.That(lt.GetValue(1.0), Is.EqualTo("a"));
            Assert.That(lt.GetValue(2.0), Is.EqualTo("b"));
            Assert.That(lt.GetValue(3.0), Is.EqualTo("c"));
        }

        [Test]
        public void TestTables_SimpleArray4()
        {
            dynamic t = engine.Execute(@"return { false, true, 42, 'abc', {}, function() return 12.3 end }");
            Assert.That(t, Is.TypeOf<LuaTable>());
            var lt = (LuaTable)t;
            Assert.That(lt.Length(), Is.EqualTo(6));
            Assert.That(lt.GetValue(1.0), Is.False);
            Assert.That(lt.GetValue(2.0), Is.True);
            Assert.That(lt.GetValue(3.0), Is.EqualTo(42));
            Assert.That(lt.GetValue(4.0), Is.EqualTo("abc"));
            Assert.That(lt.GetValue(5.0), Is.TypeOf<LuaTable>());
            Assert.That(lt.GetValue(6.0), Is.TypeOf<Func<dynamic>>());
            Assert.That(((Func<dynamic>)lt.GetValue(6.0))(), Is.EqualTo(12.3));
        }

        [Test]
        public void TestTables_Dict1()
        {
            dynamic t = engine.Execute(@"return { a = 1, b = 2, c = 3 }");
            Assert.That(t, Is.TypeOf<LuaTable>());
            var lt = (LuaTable)t;
            Assert.That(lt.Length(), Is.EqualTo(0));
            Assert.That(lt.GetValue("a"), Is.EqualTo(1.0));
            Assert.That(lt.GetValue("b"), Is.EqualTo(2.0));
            Assert.That(lt.GetValue("c"), Is.EqualTo(3.0));
        }

        [Test]
        public void TestTables_Dict2()
        {
            dynamic t = engine.Execute(@"return { a = 'x', b = 'y', c = 'z' }");
            Assert.That(t, Is.TypeOf<LuaTable>());
            var lt = (LuaTable)t;
            Assert.That(lt.Length(), Is.EqualTo(0));
            Assert.That(lt.GetValue("a"), Is.EqualTo("x"));
            Assert.That(lt.GetValue("b"), Is.EqualTo("y"));
            Assert.That(lt.GetValue("c"), Is.EqualTo("z"));
        }

        [Test]
        public void TestTables_Dict3()
        {
            dynamic t = engine.Execute(@"return { a = false, b = true, c = 42, d = 'abc', e = {}, f = function() return 12.3 end }");
            Assert.That(t, Is.TypeOf<LuaTable>());
            var lt = (LuaTable)t;
            Assert.That(lt.Length(), Is.EqualTo(0));
            Assert.That(lt.GetValue("a"), Is.False);
            Assert.That(lt.GetValue("b"), Is.True);
            Assert.That(lt.GetValue("c"), Is.EqualTo(42.0));
            Assert.That(lt.GetValue("d"), Is.EqualTo("abc"));
            Assert.That(lt.GetValue("e"), Is.TypeOf<LuaTable>());
            Assert.That(lt.GetValue("f"), Is.TypeOf<Func<dynamic>>());
            Assert.That(((Func<dynamic>)lt.GetValue("f"))(), Is.EqualTo(12.3));
        }

        [Test]
        public void TestTables_DictArray1()
        {
            dynamic t = engine.Execute(@"return { [1] = false, [2] = true, [3] = 42, [4] = 'abc', [5] = {}, [6] = function() return 12.3 end }");
            Assert.That(t, Is.TypeOf<LuaTable>());
            var lt = (LuaTable) t;
            Assert.That(lt.Length(), Is.EqualTo(6));
            Assert.That(lt.GetValue(1.0), Is.False);
            Assert.That(lt.GetValue(2.0), Is.True);
            Assert.That(lt.GetValue(3.0), Is.EqualTo(42));
            Assert.That(lt.GetValue(4.0), Is.EqualTo("abc"));
            Assert.That(lt.GetValue(5.0), Is.TypeOf<LuaTable>());
            Assert.That(lt.GetValue(6.0), Is.TypeOf<Func<dynamic>>());
            Assert.That(((Func<dynamic>)lt.GetValue(6.0))(), Is.EqualTo(12.3));
        }

        [Test]
        public void TestTables_DictArray2()
        {
            dynamic t = engine.Execute(@"return { ['a'] = false, [2] = true, ['c'] = 42, [3] = 'abc', ['e'] = {}, }");
            Assert.That(t, Is.TypeOf<LuaTable>());
            var lt = (LuaTable)t;
            Assert.That(lt.Length(), Is.EqualTo(0));
            Assert.That(lt.GetValue("a"), Is.False);
            Assert.That(lt.GetValue(2.0), Is.True);
            Assert.That(lt.GetValue("c"), Is.EqualTo(42.0));
            Assert.That(lt.GetValue(3.0), Is.EqualTo("abc"));
            Assert.That(lt.GetValue("e"), Is.TypeOf<LuaTable>());
        }

        [Test]
        public void TestTables_DictArray3()
        {
            dynamic t = engine.Execute(@"return { 'ab', 'cd', ['x'] = 'ef', ['y'] = 'gh', }");
            Assert.That(t, Is.TypeOf<LuaTable>());
            var lt = (LuaTable)t;
            Assert.That(lt.Length(), Is.EqualTo(2));
            Assert.That(lt.GetValue(1.0), Is.EqualTo("ab"));
            Assert.That(lt.GetValue(2.0), Is.EqualTo("cd"));
            Assert.That(lt.GetValue("x"), Is.EqualTo("ef"));
            Assert.That(lt.GetValue("y"), Is.EqualTo("gh"));
        }

        [Test]
        public void TestTables_DictArray4()
        {
            dynamic t = engine.Execute(@"return { ['x'] = 'ab', 'cd', ['y'] = 'ef', 'gh' }");
            Assert.That(t, Is.TypeOf<LuaTable>());
            var lt = (LuaTable)t;
            Assert.That(lt.Length(), Is.EqualTo(2));
            Assert.That(lt.GetValue(1.0), Is.EqualTo("cd"));
            Assert.That(lt.GetValue(2.0), Is.EqualTo("gh"));
            Assert.That(lt.GetValue("x"), Is.EqualTo("ab"));
            Assert.That(lt.GetValue("y"), Is.EqualTo("ef"));
        }

        [Test]
        public void TestTables_DictArray5()
        {
            dynamic t = engine.Execute(@"return { [5] = 'ab', ['x'] = 'cd', 'ef', ['y'] = 'gh', 'ij' }");
            Assert.That(t, Is.TypeOf<LuaTable>());
            var lt = (LuaTable)t;
            Assert.That(lt.Length(), Is.EqualTo(2));
            Assert.That(lt.GetValue(1.0), Is.EqualTo("ef"));
            Assert.That(lt.GetValue(2.0), Is.EqualTo("ij"));
            Assert.That(lt.GetValue("x"), Is.EqualTo("cd"));
            Assert.That(lt.GetValue("y"), Is.EqualTo("gh"));
            Assert.That(lt.GetValue(5.0), Is.EqualTo("ab"));
        }

        #endregion

        #region Metamethod Tests

        [Test]
        public void TestTables_IndexMetamethod()
        {
            string code =
@"
t = { a = 10, b = 'hello' }
mt = { }
mt.__index =    function (table,index)
                    if index == 'string' then return table.b else return table.a end
                end
setmetatable(t,mt)
assert(t['string'] == 'hello')
out2 = t.double
";

            dynamic table = PerformTableTest(code);
            var out1 = table["string"];
            var out2 = table.dbl;
            Assert.That(out1 == "hello", Is.True);
            Assert.That(out2 == 10.0, Is.True);
        }
        

       

        #endregion
    }
}
