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
System.Math=clr.import('System.Math')
assert(clr.call(System.Math,'Pow',10,2) == 100)
assert(System.Math.Pow(10,2) == 100)
assert(System.Math['Pow'](10,2) == 100)
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


        [Test]
        public void TestStructAccess()
        {           
            string code =
"teststruct=clr.import('" + typeof(TestStruct).AssemblyQualifiedName + "')" +
@"
assert(type(teststruct) == 'table','Failed to import TestStruct')

ts1=teststruct()
assert(clr.getvalue(ts1,'x') == 0, 'Failed ts1.x == 0')
assert(clr.getvalue(ts1,'y') == 0, 'Failed ts1.y == 0')
assert(clr.getvalue(ts1,'z') == nil, 'Failed ts1.z == nil')

clr.setvalue(ts1,'x',10)
ts1['y'] = 5
ts1['z'] = '15'
assert(clr.getvalue(ts1,'x') == 10,'Failed ts1.x == 10')
assert(ts1.y == 5,'Failed ts1.y == 5')
assert(ts1['z'] == '15','Failed ts1.z == tostring(15)')
";

            engine.Execute(code);
        }

        public struct TestStruct
        {
            public TestStruct(double _x, double _y)
            {
                x = _x;
                y = _y;
                __z = null;
            }

            public TestStruct(string _z)
            {
                x = y = 0;
                __z = _z;
            }

            public double x, y;

            private string __z;
            public string z
            { get { return __z; } set{ __z = value; } }
        }
    }
}
