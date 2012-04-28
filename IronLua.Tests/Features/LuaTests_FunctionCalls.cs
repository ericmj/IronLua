using System;
using IronLua.Hosting;
using Microsoft.Scripting;
using Microsoft.Scripting.Hosting;
using NUnit.Framework;

namespace IronLua.Tests.Features
{
    // ReSharper disable InconsistentNaming
    // ReSharper disable ConvertToConstant.Local

    [TestFixture]
    public class LuaTests_FunctionCalls
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

        [Test]
        public void TestFunction_print_HelloWorld1()
        {
            string code = "print \"Hello World\"";

            PerformTest(code, "Hello World");
        }

        [Test]
        public void TestFunction_print_HelloWorld2()
        {
            string code = "print 'Hello World'";

            PerformTest(code, "Hello World");
        }

        [Test]
        public void TestFunction_print_HelloWorld3()
        {
            string code = "print [[Hello World]]";

            PerformTest(code, "Hello World");
        }

        [Test]
        public void TestFunction_print_HelloWorld4()
        {
            string code = "print('Hello World')";

            PerformTest(code, "Hello World");
        }

        [Test]
        public void TestFunction_print_Number314()
        {
            string code = "print(3.14)";

            PerformTest(code, "3.14");
        }


        public void PerformStartingTest(string code, string expect)
        {
            string output, error;
            dynamic result = engine.ExecuteTestCode(code, out output, out error);

            Assert.That((object)result, Is.Null);
            Assert.That(output, Is.StringStarting(expect) & Is.StringEnding(Environment.NewLine));
            Assert.That(error, Is.Empty);
        }

        [Test]
        public void TestFunction_print_EmptyTable()
        {
            string code = "print {}";

            PerformStartingTest(code, "table: ");
        }

        [Test]
        public void TestFunction_print_TableXY()
        {
            string code = "print { x = 5, y = 10 }";

            PerformStartingTest(code, "table: ");
        }

        [Test]
        public void TestFunction_print_VariableIsTableXY()
        {
            string code = @"
x = { x = 5, y = 10 }
print(x)";
            PerformStartingTest(code, "table: ");
        }

        [Test]
        public void TestFunction_print_VariableIsFunction()
        {
            string code = @"
x = function() end
print(x)";
            PerformStartingTest(code, "function: ");
        }

        [Test]
        public void TestFunction_print_VariableIsNumber314()
        {
            string code = @"
x = 3.14
print(x)";
            PerformTest(code, "3.14");
        }

        [Test]
        public void TestFunction_print_VariableIsStringHelloWorld()
        {
            string code = @"
x = 'Hello World'
print(x)";
            PerformTest(code, "Hello World");
        }

        [Test]
        public void TestFunction_print__VERSION()
        {
            string code = "print(_VERSION)";

            // TODO: We should get version from ScriptingEngine or something
            //PerformTest(code, "IronLua 0.0.1.0");
            PerformTest(code, "Lua 5.1");
        }

        [Test]
        public void TestFunction_print__G()
        {
            string code = "print(_G)";

            PerformStartingTest(code, "table: ");
        }

        [Test]
        [ExpectedException(typeof(SyntaxErrorException),
            ExpectedMessage = "function arguments expected near '42'")]
        public void TestFunction_ArgumentsExpected1()
        {
            engine.Execute("p:f 42");
        }

        [Test]
        public void TestFunction_YoBill()
        {
            string code = @"
function yo(yourname)
  local text = 'hello, '
  return text .. yourname
end
print( yo('bill') )";

            PerformTest(code, "hello, bill");
        }

        [Test]
        public void TestFunction_Factorial()
        {
            string code = @"
function fractoral(n)
  if n < 2 then
    return 1
  else
    return n * fractoral(n - 1)
  end
end
print( fractoral(6) )";

            PerformTest(code, "720");
        }
    }
}

#if false
/// Python code & AST for fractoral function

>>>def factoral(n):
...   if n < 2:
...     return 1
...   else:
...     return n * fractoral(n - 1)
...
//
// AST <undefined>
//

.codeblock Object <undefined> ( global,)() {
    .var Object fractoral (Local)

    {
        /*empty*/;
        (Void)(.bound fractoral) = (PythonOps.MakeFunction)(
            .context,
            "fractoral",
            .block (fractoral #1),
            .new String[] = {
                "n",
            },
            .new Object[] = {},
            (FunctionAttributes)None,
            .null,
            1,
            .null,
        );
    }
}
//
// CODE BLOCK: fractoral (1)
//

.codeblock Object fractoral ()(
    .arg Object n (Parameter,InParameterArray)
) {

    {
        .if (.action (Boolean) Do LessThan( // DoOperation LessThan
            (.bound n)
            2
        ) ) {{
            .return (Object)1;
        }
        } .else {{
            .return .action (Object) Do Multiply( // DoOperation Multiply
                (.bound n)
                .action (Object) Call( // CallSimple
                    (.bound fractoral)
                    .action (Object) Do Subtract( // DoOperation Subtract
                        (.bound n)
                        1
                    )
                )
            );
        }
        };
        /*empty*/;
    }
}
>>> print( fractoral(6) )
//
// AST <undefined>
//

.codeblock Object <undefined> ( global,)() {
    .var Object fractoral (Local)

    {
        /*empty*/;
        {
            (PythonOps.Print)(
                .action (Object) Call( // CallSimple
                    (.bound fractoral)
                    6
                ),
            );
        }
    }
}
720
>>>
#endif
