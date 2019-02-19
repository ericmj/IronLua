using System;
using IronLua.Hosting;
using Microsoft.Scripting.Hosting;
using NUnit.Framework;

namespace IronLua.Tests.Features
{
    // ReSharper disable InconsistentNaming
    // ReSharper disable ConvertToConstant.Local

    [TestFixture]
    public class LuaTests_IfStatements
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
            dynamic result = engine.ExecuteTestCode(code, out output, out error);

            Assert.That((object)result, Is.Null);
            Assert.That(output, Is.EqualTo(expect + Environment.NewLine));
            Assert.That(error, Is.Empty);
        }

        [Test]
        public void TestIfThen_True()
        {
            string code = @"
if true then
    print 'one'
end";
            PerformTest(code, "one");
        }

        [Test]
        public void TestIfThen_False()
        {
            string code = @"
if false then
    print 'one'
end";
            string output, error;
            engine.ExecuteTestCode(code, out output, out error);

            Assert.That(output, Is.Empty);
            Assert.That(error, Is.Empty);
        }

        [Test]
        public void TestIfThenElse_True()
        {
            string code = @"
if true then
    print 'one'
else
    print 'two'
end";
            PerformTest(code, "one");
        }

        [Test]
        public void TestIfThenElse_False()
        {
            string code = @"
if false then
    print 'one'
else
    print 'two'
end";
            PerformTest(code, "two");
        }

        [Test]
        public void TestIfThenElseIfElse_TrueTrue()
        {
            string code = @"
if true then
    print 'one'
elseif true then
    print 'two'
else
    print 'three'
end";
            PerformTest(code, "one");
        }

        [Test]
        public void TestIfThenElseIfElse_TrueFalse()
        {
            string code = @"
if true then
    print 'one'
elseif false then
    print 'two'
else
    print 'three'
end";
            PerformTest(code, "one");
        }

        [Test]
        public void TestIfThenElseIfElse_FalseTrue()
        {
            string code = @"
if false then
    print 'one'
elseif true then
    print 'two'
else
    print 'three'
end";
            PerformTest(code, "two");
        }

        [Test]
        public void TestIfThenElseIfElse_FalseFalse()
        {
            string code = @"
if false then
    print 'one'
elseif false then
    print 'two'
else
    print 'three'
end";
            PerformTest(code, "three");

        }


        [Test]
        public void TestIfThenElse_OneLtTwo()
        {
            string code = @"
if 1 < 2 then
    print 'one'
else
    print 'two'
end";
            PerformTest(code, "one");
        }

        [Test]
        public void TestIfThenElse_OneGtTwo()
        {
            string code = @"
if 1 > 2 then
    print 'one'
else
    print 'two'
end";
            PerformTest(code, "two");
        }

        [Test]
        public void TestIfThenElse_OneEqOne()
        {
            string code = @"
if 1 == 1 then
    print 'one'
else
    print 'two'
end";
            PerformTest(code, "one");
        }

        [Test]
        public void TestIfThenElse_OneEqTwo()
        {
            string code = @"
if 1 == 2 then
    print 'one'
else
    print 'two'
end";
            PerformTest(code, "two");
        }

        [Test]
        public void TestIfThenElse_OneNotEqOne()
        {
            string code = @"
if 1 ~= 1 then
    print 'one'
else
    print 'two'
end";
            PerformTest(code, "two");
        }

        [Test]
        public void TestIfThenElse_OneNotEqTwo()
        {
            string code = @"
if 1 ~= 2 then
    print 'one'
else
    print 'two'
end";
            PerformTest(code, "one");
        }

        [Test]
        public void TestIfThenElse_Number()
        {
            string code = @"
if 0.5 then
    print 'one'
else
    print 'two'
end";
            PerformTest(code, "one");
        }

        [Test]
        public void TestIfThenElse_NumberZero()
        {
            string code = @"
if 0 then
    print 'one'
else
    print 'two'
end";
            PerformTest(code, "one");
        }

        [Test]
        public void TestIfThenElse_String()
        {
            string code = @"
if 'a' then
    print 'one'
else
    print 'two'
end";
            PerformTest(code, "one");
        }

        [Test]
        public void TestIfThenElse_StringEmpty()
        {
            string code = @"
if '' then
    print 'one'
else
    print 'two'
end";
            PerformTest(code, "one");
        }

        [Test]
        public void TestIfThenElse_nil()
        {
            string code = @"
if nil then
    print 'one'
else
    print 'two'
end";
            PerformTest(code, "two");
        }

        [Test]
        public void TestIfThenElseIfElse_nilnil()
        {
            string code = @"
if nil then
    print 'one'
elseif nil then
    print 'two'
else
    print 'three'
end";
            PerformTest(code, "three");
        }

        [Test]
        public void TestIfThenElse_VariableIsNil()
        {
            string code = @"
local x = nil
if x then
    print 'one'
else
    print 'two'
end";
            PerformTest(code, "two");
        }

        [Test]
        public void TestIfThenElse_VariableThatDoesNotExist()
        {
            string code = @"
if xxx then
    print 'one'
else
    print 'two'
end";
            PerformTest(code, "two");
        }

        [Test]
        public void TestIfThenElse_VariableIsTrue()
        {
            string code = @"
local x = true
if x then
    print 'one'
else
    print 'two'
end";
            PerformTest(code, "one");
        }

        [Test]
        public void TestIfThenElse_VariableIsFalse()
        {
            string code = @"
local x = false
if x then
    print 'one'
else
    print 'two'
end";
            PerformTest(code, "two");
        }

        [Test]
        public void TestIfThenElse_VariableIsString()
        {
            string code = @"
local x = 'bla'
if x then
    print 'one'
else
    print 'two'
end";
            PerformTest(code, "one");
        }

        [Test]
        public void TestIfThenElse_VariableIsNumber()
        {
            string code = @"
local x = 3.14
if x then
    print 'one'
else
    print 'two'
end";
            PerformTest(code, "one");
        }

        [Test]
        public void TestIfThenElse_VariableIsFunction()
        {
            string code = @"
local x = function() end
if x then
    print 'one'
else
    print 'two'
end";
            PerformTest(code, "one");
        }

        [Test]
        public void TestIfThenElse_VariableIsEmptyTable()
        {
            string code = @"
local x = {}
if x then
    print 'one'
else
    print 'two'
end";
            PerformTest(code, "one");
        }

        [Test]
        public void TestIfThenElse_VariableIsFunctionCallOne()
        {
            string code = @"
local f = function() return true end
if f() then
    print 'one'
else
    print 'two'
end";
            PerformTest(code, "one");
        }

        [Test]
        public void TestIfThenElse_VariableIsFunctionCallTwo()
        {
            string code = @"
local f = function() return false end
if f() then
    print 'one'
else
    print 'two'
end";
            PerformTest(code, "two");
        }

        [Test]
        public void TestIfThenElse_CheckOrderOfEvaluation_One()
        {
            string code = @"
function f(x,y)
    print(x)
    return y
end
if f('one',true) then
elseif f('two',true) then
elseif f('three',true) then
else print('four')
end";
            PerformTest(code, String.Join(Environment.NewLine, new[] { "one" }));
        }

        [Test]
        public void TestIfThenElse_CheckOrderOfEvaluation_Two()
        {
            string code = @"
function f(x,y)
    print(x)
    return y
end
if f('one',false) then
elseif f('two',true) then
elseif f('three',true) then
else print('four')
end";
            PerformTest(code, String.Join(Environment.NewLine, new[] { "one", "two" }));
        }

        [Test]
        public void TestIfThenElse_CheckOrderOfEvaluation_Three()
        {
            string code = @"
function f(x,y)
    print(x)
    return y
end
if f('one',false) then
elseif f('two',false) then
elseif f('three',true) then
else print('four')
end";
            PerformTest(code, String.Join(Environment.NewLine, new[] { "one", "two", "three" }));
        }

        [Test]
        public void TestIfThenElse_CheckOrderOfEvaluation_Four()
        {
            string code = @"
function f(x,y)
    print(x)
    return y
end
if f('one',false) then
elseif f('two',false) then
elseif f('three',false) then
else print('four')
end";
            PerformTest(code, String.Join(Environment.NewLine, new[] { "one", "two", "three", "four" }));
        }

    }
}
