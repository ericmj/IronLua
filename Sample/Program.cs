using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using IronLua.Compiler;
using IronLua.Library;
using IronLua.Runtime;
using Expr = System.Linq.Expressions.Expression;
using ParamExpr = System.Linq.Expressions.ParameterExpression;
using System.Text;
using IronLua.Compiler.Parser;
using Mono.Linq.Expressions;

namespace Sample
{
    public class Program
    {
        static void Main(string[] args)
        {
            new Program().Test1();
        }

        void Test1()
        {
            var input = new Input(@"for x=-1,-10,-2 do y="""" end");
            var parser = new Parser(input);
            var context = new Context();

            var ast = parser.Parse();
            var gen = new Generator(context);
            var expr = gen.Compile(ast);
            var func = expr.Compile();
            func();
        }

        void Test2()
        {
            var debugInfoGen = DebugInfoGenerator.CreatePdbGenerator();
            var symbolDocument = Expr.SymbolDocument("temp_file.file");

            var asm = AppDomain.CurrentDomain.DefineDynamicAssembly(new AssemblyName("test.dll"), AssemblyBuilderAccess.RunAndCollect);
            var module = asm.DefineDynamicModule("test", "test.dll", true);
            var type = module.DefineType("Generated", TypeAttributes.Public);
            var method = type.DefineMethod("Main", MethodAttributes.Public | MethodAttributes.Static);

            var xParam = Expr.Variable(typeof(int), "xParam");
            var yParam = Expr.Variable(typeof(object), "yParam");
            var secondLambda = Expr.Variable(typeof(Action), "secondLambda");
            var thirdLambda = Expr.Variable(typeof(Action), "thirdLambda");

            var expr =
                Expr.Lambda(
                    Expr.Block(
                        new[] { xParam, yParam, secondLambda, thirdLambda },
                        Expr.DebugInfo(symbolDocument, 1, 1, 1, 10),
                        Expr.Assign(
                            thirdLambda,
                            Expr.Lambda(Expr.Throw(Expr.New(typeof(Exception))))),
                        Expr.DebugInfo(symbolDocument, 2, 1, 2, 10),
                        Expr.Assign(secondLambda, Expr.Lambda(Expr.Call(typeof(Program).GetMethod("Throw", BindingFlags.Static | BindingFlags.Public ), thirdLambda))),
                        Expr.DebugInfo(symbolDocument, 3, 1, 3, 10),
                        Expr.Invoke(secondLambda),
                        Expr.DebugInfo(symbolDocument, 3, 1, 3, 10),
                        Expr.RuntimeVariables(xParam, yParam)),
                    new ParamExpr[] {});
            Console.Out.WriteLine(expr.ToCSharpCode());
            expr.CompileToMethod(method, debugInfoGen);
            var newType = type.CreateType();
            try
            {
                var result = newType.GetMethod("Main").Invoke(null, new object[0]);
            } catch (TargetInvocationException e)
            {
                var inner = e.InnerException;
                var str = inner.StackTrace;
                var st = new StackTrace(e.InnerException, true);
            }
        }

        public static void Throw(Action func)
        {
            func();
            var st = new StackTrace(true);
        }

        static void Test3()
        {
            var y = -5;
            var x = (uint)y;
            string s = LuaString.Format("%e", -65.0);
        }
    }
}
