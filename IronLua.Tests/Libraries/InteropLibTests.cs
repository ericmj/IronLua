using System;
using IronLua.Hosting;
using IronLua.Runtime;
using Microsoft.Scripting.Hosting;
using NUnit.Framework;

namespace IronLua.Tests.Libraries
{
    [TestFixture]
    public class InteropLibTests
    {
        ScriptEngine engine;

        [TestFixtureSetUp]
        public void PrepareEngine()
        {
            engine = Lua.CreateEngine();
        }
                
        [Test]
        public void TestImport()
        {
            string code = 
@"
clrmath = clr.import('System.Math')
clrpow = clr.method(clrmath,'Pow')

test1=clrpow(10,2)
assert(test1 == 100)

test2=clr.call(clrmath,'Pow',10,2)
assert(test2 == 100)

test3 = clr.call(clr.import('System.Math'),'Pow',10,2)
assert(test3 == 100)
";
                        
            engine.Execute(code);
            
        }

        [Test]
        public void TestTypeInstantiation()
        {
            string code =
@"
object=clr.import('System.Object')
objInstance=object()
assert(not(objInstance == nil))

str=clr.import('System.String')
strInstance=clr.call(str,'Copy','This is a')
strInstance=clr.call(str,'Concat',strInstance,' test string')
assert(not(strInstance == nil))
assert(tostring(strInstance) == 'This is a test string')
";

            engine.Execute(code);
        }
                
        [Test]
        public void TestGlobalNamespaces()
        {
            string code =
@"
System={}
System.Math=clr.import('System.Math',true)
assert(clr.call(System.Math,'Pow',10,2) == 100)
";

            var scope = engine.CreateScope();
            var context = engine.GetLuaContext();

            engine.Execute(code, scope);
        }

        /// <summary>
        /// Tests recursive printing of a table's contents, requires System.Console for indentation
        /// </summary>
        [Test]
        public void TestTableRecursion()
        {
            string code =
@"
Console=clr.import('System.Console')
indent =          function (indentation)
                    for i=1,indentation do clr.call(Console,'Write',' ') end
                  end
printChildren =   function (ns,indentation)
                        for k,v in pairs(ns) do
                            indent(indentation)
                            print(tostring(k))
                            if type(v) == 'table' and not(tostring(k) == '_ENV') and not(tostring(k) == '_G') then
                                printChildren(v,indentation + 2)
                            else
                                indent(indentation + 4)
                                print(tostring(v))
                            end
                        end
                    end

printChildren(_ENV,0)
";

            engine.Execute(code);
        }
    }
}
