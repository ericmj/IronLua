using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using IronLua.Compiler;
using IronLua.Compiler.Parser;
using IronLua.Hosting;
using IronLua.Runtime;
using Microsoft.Scripting;
using NUnit.Framework;

namespace IronLua.Tests.Compiler
{
    [TestFixture]
    public class ParserTests
    {

        TextReader SafeOpenReader(string f)
        {            
            try
            {
                return File.OpenText(f);
            }
            catch (FileNotFoundException)
            {
                Assert.Ignore("File not found");
                return null;
            }
        }

        public string Repeat(char s, int n)
        {
            if (n <= 0)
                return String.Empty;

            var sb = new StringBuilder(n);
            for (int i = 0; i < n; ++i)
            {
                sb.Append(s);
            }
            return sb.ToString();
        }

        public string Repeat(string s, int n)
        {
            if (n <= 0)
                return String.Empty;

            var sb = new StringBuilder(n*s.Length);
            for (int i = 0; i < n; ++i)
            {
                sb.Append(s);
            }
            return sb.ToString();
        }

        [Test, TestCaseSource(typeof(LuaTestSuiteSource), "LuaTestCases")]
        public void ParserErrorReportTests(string luaFile)
        {
            //var reader = SafeOpenReader(luaFile);
            var options = new LuaCompilerOptions() {SkipFirstLine = true};

            var engine = Lua.CreateEngine();            
            var context = Lua.GetLuaContext(engine);
            var sourceUnit = context.CreateFileUnit(luaFile);
            var reader = sourceUnit.GetReader();

            var tokenizer = new Tokenizer(ErrorSink.Default, options);
            tokenizer.Initialize(null, reader, sourceUnit, SourceLocation.MinValue);
            var parser = new Parser(tokenizer, tokenizer.ErrorSink);

            try
            {
                var ast = parser.Parse();
                Assert.That(ast, Is.Not.Null);            
            } 
            catch (SyntaxErrorException ex)
            {
                Console.WriteLine("Source File     : {0}", new Uri(ex.SourcePath));
                Console.WriteLine("Source Location : {0}", ex.RawSpan);
                Console.WriteLine("Source CodeLine : {0}", ex.GetCodeLine());
                Console.WriteLine("Error {1,-9} : {0}^", Repeat('=', ex.Column - 1), ex.ErrorCode);                                       
                
                throw;
            }
        }

        [Test]
        public void MyTest()
        {
            ParserErrorReportTests(@"F:\workspace\DLR\IronLua-github\lua-5.2.0-tests\all.lua");
        }

        public static class LuaTestSuiteSource
        {
            const string LuaTestSuitePath = @"F:\workspace\DLR\IronLua-github\lua-5.2.0-tests";
            //const string LuaTestSuitePath = @"F:\workspace\DLR\IronLua-github\lua-5.1-tests";

            public static string[] LuaTestSuiteFiles = new[]
            {
                "all.lua",
                "api.lua",
                "attrib.lua",
                "big.lua",
                "bitwise.lua",
                "calls.lua",
                "checktable.lua",
                "closure.lua",
                "code.lua",
                "constructs.lua",
                "coroutine.lua",
                "db.lua",
                "errors.lua",
                "events.lua",
                "files.lua",
                "gc.lua",
                "goto.lua",
                "literals.lua",
                "locals.lua",
                "main.lua",
                "math.lua",
                "nextvar.lua",
                "pm.lua",
                "sort.lua",
                "strings.lua",
                "vararg.lua",
                "verybig.lua"
            };

            public static IEnumerable<TestCaseData> LuaTestCases()
            {
                return LuaTestSuiteFiles
                    .Select(f => new TestCaseData(Path.Combine(LuaTestSuitePath, f)).SetName(f));
            }
        }
    }
}
