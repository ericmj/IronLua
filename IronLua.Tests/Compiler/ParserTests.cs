﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using IronLua.Compiler;
using IronLua.Compiler.Parser;
using IronLua.Hosting;
using Microsoft.Scripting;
using NUnit.Framework;

namespace IronLua.Tests.Compiler
{
    //
    // These tests uses the "official" Lua Test Suite available at: 
    //   http://www.lua.org/tests/5.2
    // compressed in these files:
    //   http://www.lua.org/tests/5.2/lua-5.2.0-tests.tar.gz
    //   http://www.inf.puc-rio.br/~roberto/lua/lua5.1-tests.tar.gz
    //
    // These tests were written for the Lua environment, but we use
    // them here to test the Lexer/Parser/Generator. Since we don't 
    // execute them here, the test files are only an indication that
    // the compiler can work on the files without throwing syntax
    // errors. They don't test the error cases and error messages.
    //
    // Usage: 
    // - Download the files described above
    // - Unpack them to a folder.
    // - Update the two paths in the LuaTestSuiteSource class (see below)
    // - Compile and execute the test in NUnit or RSharper.
    // 
    [TestFixture]
    public class ParserTests
    {
        public static TextReader OpenReaderOrIgnoreTest(Func<TextReader> getReader)
        {            
            try
            {
                return getReader();
            }
            catch (DirectoryNotFoundException)
            {
                Assert.Ignore("Directory not found");
                return null;
            }
            catch (FileNotFoundException)
            {
                Assert.Ignore("File not found");
                return null;
            }
        }

        public static string Repeat(char c, int n)
        {
            if (n <= 0)
                return String.Empty;

            var sb = new StringBuilder(n);
            for (int i = 0; i < n; ++i)
            {
                sb.Append(c);
            }
            return sb.ToString();
        }

        public static string Repeat(string s, int n)
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

        public static void AssertSyntaxError(Action action)
        {
            try
            {
                action();
            }
            catch (SyntaxErrorException ex)
            {
                // Display a pretty picture of the syntax error exception 
                Console.WriteLine("Source File     : {0}", new Uri(ex.SourcePath));
                Console.WriteLine("Source Location : {0}", ex.RawSpan);
                Console.WriteLine("Source CodeLine : {0}", ex.GetCodeLine());
                Console.WriteLine("Error {1,-9} : {0}^", ParserTests.Repeat('=', ex.Column - 1), ex.ErrorCode);

                throw; // so test can fail
            }
        }

        public void ParserErrorReportTests(string luaFile, bool useLua52)
        {
            var options = new LuaCompilerOptions()
            {
                SkipFirstLine = true,
                UseLua52Features = useLua52,
            };

            var engine = Lua.CreateEngine();            
            var context = Lua.GetLuaContext(engine);
            var sourceUnit = context.CreateFileUnit(luaFile);

            //var reader = OpenReaderOrIgnoreTest(() => File.OpenText(luaFile));
            var reader = OpenReaderOrIgnoreTest(sourceUnit.GetReader);

            var tokenizer = new Tokenizer(ErrorSink.Default, options);
            tokenizer.Initialize(null, reader, sourceUnit, SourceLocation.MinValue);
            var parser = new Parser(tokenizer, tokenizer.ErrorSink, options);

            AssertSyntaxError(() =>
            {
                var ast = parser.Parse();
                Assert.That(ast, Is.Not.Null);
            });
        }

        [Test, TestCaseSource(typeof(LuaTestSuiteSource), "Lua52TestCases")]
        public void ParserTestOnLua52TestSuite(string luaFile)
        {
            ParserErrorReportTests(luaFile, useLua52:true);
        }

        [Test, TestCaseSource(typeof(LuaTestSuiteSource), "Lua51TestCases")]
        public void ParserTestOnLua51TestSuite(string luaFile)
        {
            ParserErrorReportTests(luaFile, useLua52:false);
        }

        public static class LuaTestSuiteSource
        {
            const string Lua52TestSuitePath = @"F:\workspace\DLR\IronLua-github\lua-5.2.0-tests";
            const string Lua51TestSuitePath = @"F:\workspace\DLR\IronLua-github\lua-5.1-tests";

            public static string[] LuaTestSuiteFiles = new[]
            {
                "all.lua",
                "api.lua",
                "attrib.lua",
                "big.lua",
                "calls.lua",
                "checktable.lua",
                "closure.lua",
                "code.lua",
                "constructs.lua",
                "db.lua",
                "errors.lua",
                "events.lua",
                "files.lua",
                "gc.lua",
                "literals.lua",
                "locals.lua",
                "main.lua",
                "math.lua",
                "nextvar.lua",
                "pm.lua",
                "sort.lua",
                "strings.lua",
                "vararg.lua",
                "verybig.lua",
                // Lua 5.2 specific files
                "bitwise.lua", 
                "coroutine.lua",
                "goto.lua",
            };

            public static IEnumerable<TestCaseData> LuaTestCases(string path)
            {
                return LuaTestSuiteFiles 
                    .Select(f => new TestCaseData(Path.Combine(path, f)).SetName(f));
            }

            public static IEnumerable<TestCaseData> Lua52TestCases()
            {
                return LuaTestCases(Lua52TestSuitePath);
            }

            public static IEnumerable<TestCaseData> Lua51TestCases()
            {
                return LuaTestCases(Lua51TestSuitePath).Take(24);
            }
        }
    }
}
