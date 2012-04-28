using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using IronLua.Compiler;
using IronLua.Compiler.Parsing;
using Microsoft.Scripting;
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
            var reader = TestUtils.OpenReaderOrIgnoreTest(() => File.OpenText(luaFile));
            Console.WriteLine("Reading data from {0}", luaFile);

            var tokenizer = new Tokenizer(ErrorSink.Default, new LuaCompilerOptions() { SkipFirstLine = true });
            tokenizer.Initialize(null, reader, null, SourceLocation.MinValue);

            var fname = @"C:\tmp\tokenizer.txt";
            using (var fout = File.CreateText(fname))
            {
                Token token;
                while ((token = tokenizer.GetNextToken()).Symbol != Symbol.Eof)
                {
                    if (token.Symbol == Symbol.Whitespace)
                        continue;
                    if (token.Symbol == Symbol.EndOfLine)
                        continue;

                    var span = tokenizer.CurrentTokenSpan();

                    fout.Write("{0,-12}", token.Symbol);
                    fout.Write("{0,-10}", span.Start);
                    fout.Write("{0,-10}", span.End);
                    {
                        fout.Write(tokenizer.CurrentTokenString());
                        //fout.Write(tokenizer.CurrentTokenValue());
                    }

                    if (token.Symbol == Symbol.String
                        && tokenizer.CurrentTokenString().Contains("\\z"))
                    {
                        //fout.WriteLine();
                        //fout.Write("LINE1: {0}", tokenizer.CurrentTokenString());
                        fout.WriteLine();
                        fout.Write("LINE2: {0}", tokenizer.CurrentTokenValue());
                    }

                    fout.WriteLine();
                }
            }
            Console.WriteLine("Written results to {0}", fname);
        }

        public void RunLexerOnLuaTestSuiteFile(string luaFile, bool useLua52)
        {
            var options = new LuaCompilerOptions()
            {
                SkipFirstLine = true,
                UseLua52Features = useLua52,
            };

            var reader = TestUtils.OpenReaderOrIgnoreTest(() => File.OpenText(luaFile));

            var tokenizer = new Tokenizer(ErrorSink.Default, options);

            Token token;
            int counter = 0;

            var sw = new Stopwatch();
            sw.Start();

            tokenizer.Initialize(null, reader, null, SourceLocation.MinValue);
            while ((token = tokenizer.GetNextToken()).Symbol != Symbol.Eof)
            {
                counter++;
            }

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
