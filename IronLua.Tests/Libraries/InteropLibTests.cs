using System;
using IronLua.Hosting;
using IronLua.Runtime;
using Microsoft.Scripting.Hosting;
using NUnit.Framework;

namespace IronLua.Tests
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

ts2=teststruct(10,2)
assert(ts2.x == 10)
assert(ts2.y == 2)
assert(ts2.z == nil)

ts3=teststruct('test')
assert(ts3.x == 0)
assert(ts3.y == 0)
assert(ts3.z == 'test')
";

            engine.Execute(code);
        }

        [Test]
        public void TestClassAccess()
        {
            string code =
"testclassNS='" + typeof(TestClass).AssemblyQualifiedName + "'" +
@"testclass=clr.import(testclassNS)
assert(type(testclass) == 'table','Failed to import TestClass')

tc1 = testclass()
assert(type(tc1) == 'IronLua.Tests.InteropLibTests+TestClass', 'Failed to instantiate TestClass')

tc1.Field = 'this is a string'
tc1.Property = 12.0
methodResult = tc1.Method()
assert(type(tc1.IndexedField) == 'boolean','tc1.IndexedField is of incorrect type')
assert(type(tc1['IndexedField']) == 'boolean', 'tc1[IndexedField] is of incorrect type')
assert(not tc1.IndexedField, 'Accessed field indexer instead of field')
assert(tc1['IndexedField'], 'Accessed field instead of field indexer')

assert(tc1.Field == 'this is a string')
assert(tc1.Property == 12)
assert(methodResult == 'this is a string:12')
";

            var scope = engine.CreateScope();

            engine.Execute(code, scope);
        }

        [Test]
        public void TestEvents()
        {
            string code =
"testclassNS='" + typeof(TestEventsClass).AssemblyQualifiedName + "'" +
@"testclass=clr.import(testclassNS)
assert(type(testclass) == 'table','Failed to import TestEventsClass')

host = testclass()
handler = function (arg) print('Event Triggered: '..tostring(arg)) end
handler2 = function (arg,e) print('EventHandler Triggered: '..tostring(arg)) end

clr.subscribe(host,'Event',handler)
clr.subscribe(host,'EventHandlerEvent',handler2)
clr.subscribe(host,'EventHandlerEvent2',handler2)
host.Trigger('Argument')
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

        public class TestClass
        {
            public string Field = "";
            public double Property
            { get; set; }

            public bool IndexedField = false;
            public bool this[object key]
            {
                get { return true; }
            }

            public object Method()
            {
                return Field + ":" + Property;
            }
        }

        public class TestEventsClass
        {
            public event Func<object, object> Event = null;

            public event EventHandler EventHandlerEvent = null;

            public event EventHandler<EventArgs> EventHandlerEvent2 = null;

            public void Trigger(object arg)
            {
                if (Event != null)
                    Event(arg);

                if (EventHandlerEvent != null)
                    EventHandlerEvent(arg, new EventArgs());

                if (EventHandlerEvent2 != null)
                    EventHandlerEvent2(arg, new EventArgs());
            }
        }
    }
}
