using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using IronLua.Compiler;
using IronLua.Compiler.Parsing;
using IronLua.Hosting;
using Microsoft.Scripting;
using Microsoft.Scripting.Runtime;
using NUnit.Framework;

namespace IronLua.Tests.Compiler
{
    // Note: See ParserTests class for some documentation

    [TestFixture]
    public class TokenizerTests
    {
        [Test, Explicit]
        public void FirstTest()
        {
            var luaFile = TestUtils.GetTestPath(@"lua-5.2.0-tests\literals.lua");

            var engine = Lua.CreateEngine();
            var context = Lua.GetLuaContext(engine);
            var unit = context.CreateFileUnit(luaFile);
            var reader = TestUtils.OpenReaderOrIgnoreTest(unit.GetReader);
            Console.WriteLine("Reading data from {0}", new Uri(luaFile));
            
            var tokenizer = new Tokenizer(ErrorSink.Default, new LuaCompilerOptions() { SkipFirstLine = true });
            tokenizer.Initialize(null, reader, unit, SourceLocation.MinValue);

            var fname = @"C:\tmp\tokenizer.txt";
            using (var fout = File.CreateText(fname))
            {
                foreach (var token in tokenizer.EnumerateTokens().TakeWhile(t => t.Symbol != Symbol.Eof))
                {
                    if (token.Symbol == Symbol.Whitespace)
                        continue;
                    if (token.Symbol == Symbol.EndOfLine)
                        continue;

                    fout.Write("{0,-12}", token.Symbol);
                    fout.Write("{0,-10}", token.Span.Start);
                    fout.Write("{0,-10}", token.Span.End);
                    fout.Write("{0}", token.Lexeme);
                    
                    fout.WriteLine();
                }
            }
            Console.WriteLine("Written results to {0}", new Uri(fname));
        }

        public void RunLexerOnLuaTestSuiteFile(string luaFile, bool useLua52)
        {
            var options = new LuaCompilerOptions()
            {
                SkipFirstLine = true,
                UseLua52Features = useLua52,
            };

            var engine = Lua.CreateEngine();
            var context = Lua.GetLuaContext(engine);
            var unit = context.CreateFileUnit(luaFile);
            var reader = TestUtils.OpenReaderOrIgnoreTest(unit.GetReader);

            var tokenizer = new Tokenizer(ErrorSink.Default, options);

            var sw = new Stopwatch();
            sw.Start();

            tokenizer.Initialize(null, reader, unit, SourceLocation.MinValue);
            int counter = tokenizer.EnumerateTokens()
                                   .TakeWhile(t => t.Symbol != Symbol.Eof)
                                   .Count();

            sw.Stop();
            Console.WriteLine("Tokenizer run: {0} ms, {1} tokens", sw.ElapsedMilliseconds, counter);
        }

        [Test, TestCaseSource(typeof(ParserTests.LuaTestSuiteSource), "Lua52TestCases")]
        public void LexerTestOnLua52TestSuite(string luaFile)
        {
            RunLexerOnLuaTestSuiteFile(luaFile, useLua52: true);
        }

        [Test, TestCaseSource(typeof(ParserTests.LuaTestSuiteSource), "Lua51TestCases")]
        public void LexerTestOnLua51TestSuite(string luaFile)
        {
            RunLexerOnLuaTestSuiteFile(luaFile, useLua52: false);
        }
    }
}
