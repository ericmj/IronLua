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
    public class LuaTests_Assignments
    {
        ScriptEngine engine;

        [TestFixtureSetUp]
        public void PrepareEngine()
        {
            engine = Lua.CreateEngine();
        }

        public ScriptScope PerformTest(string code, StringBuilder expect)
        {
            var scope = engine.CreateScope();

            string output, error;
            dynamic result = engine.ExecuteTestCode(code, scope, out output, out error);

            Assert.That((object)result, Is.Null);
            Assert.That(output, Is.EqualTo(expect.ToString()));
            Assert.That(error, Is.Empty);

            return scope;
        }

        public ScriptScope PerformVariableTest(string code, params string[] varNames)
        {
            var scope = engine.CreateScope();

            string output, error;
            dynamic result = engine.ExecuteTestCode(code, scope, out output, out error);

            Assert.That((object)result, Is.Null);
            Assert.That(output, Is.Empty);
            Assert.That(error, Is.Empty);

            foreach (var varName in varNames)
            {
                Assert.That(scope.ContainsVariable(varName), Is.True, "Variable '" + varName +"' is missing!");
            }
            return scope;
        }

        [Test]
        public void TestAssign_Number()
        {
            string code = @"a = 5";

            PerformVariableTest(code, "a");
        }

        [Test]
        public void TestAssign_String()
        {
            string code = @"a = 'hi'";

            PerformVariableTest(code, "a");
        }

        [Test]
        public void TestAssign_MultpleAssignment()
        {
            string code = @"a,b,c = 1,2,3";

            PerformVariableTest(code, "a", "b", "c");
        }

        [Test]
        public void TestAssign_SwapValues()
        {
            string code = @"a = 1; b = 2; a,b = b,a";

            PerformVariableTest(code, "a", "b");
        }

        [Test]
        public void TestAssign_TooManyValues()
        {
            string code = @"a,b = 4,5,6"; // 6 is discarded

            PerformVariableTest(code, "a", "b");
        }

        [Test]
        public void TestAssign_TooFewValues()
        {
            string code = @"a,b = 'there'"; // nil is assigned to b

            var scope = PerformVariableTest(code, "a");

            Assert.That(scope.ContainsVariable("b"), Is.False);
        }

        [Test]
        public void TestAssign_DestroyVariableByAssigningAnil()
        {
            string code = @"a = nil"; // destroy a

            var scope = PerformVariableTest(code);

            Assert.That(scope.ContainsVariable("a"), Is.False);
        }

        [Test]
        public void TestAssign_ThreeVariablesFromFunction()
        {
            string code =
                @"
function f() 
  return 1
end
a,b = f(), f()
return b
";
            var b = engine.Execute(code);
            Assert.That(b, Is.EqualTo(1.0));
        }

        [Test]
        public void TestAssign_AssignNonExistingVariableAssignsNil()
        {
            string code = @"a = xxx";

            var scope = PerformVariableTest(code);

            Assert.That(scope.ContainsVariable("a"), Is.False);
        }

        [Test]
        public void TestAssign_SimpleAdd()
        {
            string code = @"
a = 3 + 2
print(type(a))
print(a)";
            var expected = new StringBuilder();
            expected.AppendLine("number");
            expected.AppendLine("5");

            PerformTest(code, expected);
        }

        [Test]
        public void TestAssign_SimpleConcat()
        {
            string code = @"
a = '3' .. '2'
print(type(a))
print(a)";
            var expected = new StringBuilder();
            expected.AppendLine("string");
            expected.AppendLine("32");

            PerformTest(code, expected);
        }

        [Test]
        public void TestAssign_StringsConvertedToNumbers()
        {
            // numbers expected, strings are converted to numbers
            string code = @"
a = '3' + '2'
print(type(a))
print(a)";
            var expected = new StringBuilder();
            expected.AppendLine("number");
            expected.AppendLine("5");

            PerformTest(code, expected);
        }

        [Test]
        public void TestAssign_NumbersConvertedToStrings()
        {
            // strings expected, numbers are converted to strings
            string code = @"
a = 3 .. 2
print(type(a))
print(a)";
            var expected = new StringBuilder();
            expected.AppendLine("string");
            expected.AppendLine("32");

            PerformTest(code, expected);
        }

        [Test]
        public void TestAssign_EmptyTable()
        {
            // strings expected, numbers are converted to strings
            string code = @"
a = {}
print(type(a))";
//print(a)";
            var expected = new StringBuilder();
            expected.AppendLine("table");

            PerformTest(code, expected);
        }

        [Test]
        public void TestAssign_ComplexAssigmentsWithTablesAndMetatables()
        {
            string code = @"
t = { ['a'] = 1, ['b'] = 2, ['c'] = 3 }

m = {}
m.__index = function(t,k)
    local kk = k;
    if type(k) == 'string' then
        kk = string.sub(k, 1, 1)
    end
    print('index:', kk)
    return rawget(t, kk)
end
m.__newindex = function(t,k,v)
    print('newindex:', k, v)
    rawset(t,k,v)
end
setmetatable(t,m)

function f(x)
    print('f:', x)
    return x
end

t[f('x')], t[f('y')], t[f('z')] = t[f('aa')], t[f('bb')], t[f('cc')], t[f('dd')]
";
            var expected = new StringBuilder();
            expected.AppendLine("f:\tx");
            expected.AppendLine("f:\ty");
            expected.AppendLine("f:\tz");
            expected.AppendLine("f:\taa");
            expected.AppendLine("index:\ta");
            expected.AppendLine("f:\tbb");
            expected.AppendLine("index:\tb");
            expected.AppendLine("f:\tcc");
            expected.AppendLine("index:\tc");
            expected.AppendLine("f:\tdd");
            expected.AppendLine("index:\td");
            expected.AppendLine("newindex:\tz\t3");
            expected.AppendLine("newindex:\ty\t2");
            expected.AppendLine("newindex:\tx\t1");

            PerformTest(code, expected);
        }

        [Test]
        public void TestAssign_TableConstructorAssigment()
        {
            string code = @"
function f(x)
    print('f:', x)
    return x
end
t = { [f('a')] = f(1), [f('b')] = f(2), [f('c')] = f(3) }
";
            var expected = new StringBuilder();
            expected.AppendLine("f:\ta");
            expected.AppendLine("f:\t1");
            expected.AppendLine("f:\tb");
            expected.AppendLine("f:\t2");
            expected.AppendLine("f:\tc");
            expected.AppendLine("f:\t3");

            PerformTest(code, expected);
        }

        [Test]
        [ExpectedException(typeof(SyntaxErrorException),
            ExpectedMessage = "unexpected symbol near '=' (line 1, column 4)")]
        public void TestTables_AssignmentErrorMsg1()
        {
            engine.Execute("a, = 1,2");
        }
    }
}
