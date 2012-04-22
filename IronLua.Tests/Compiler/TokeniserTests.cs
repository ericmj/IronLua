using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using IronLua.Compiler;
using IronLua.Compiler.Parser;
using Microsoft.Scripting;
using NUnit.Framework;

namespace IronLua.Tests.Compiler
{
    [TestFixture]
    public class TokenizerTests
    {
        [Test]
        public void FirstTest()
        {
            //var reader = new StringReader("print(1.25)");
            var luaFile = @"F:\workspace\DLR\IronLua-github\lua-5.2.0-tests\literals.lua";
            var reader = File.OpenText(luaFile);
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

        string TestCasePath = @"F:\workspace\DLR\IronLua-github\lua-5.2.0-tests";
        //string TestCasePath = @"F:\workspace\DLR\IronLua-github\lua-5.1-tests";

        [Datapoints]
        public string[] TestCaseFiles = new[]
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

        [Theory]
        public void RunLexerOnLuaTestSuiteFile(string testCaseFile)
        {
            TextReader reader;
            try
            {
                reader = File.OpenText(Path.Combine(TestCasePath, testCaseFile));
            } 
            catch (FileNotFoundException)
            {
                Assert.Ignore( "File not found" );
                return;
            }
            var tokenizer = new Tokenizer(ErrorSink.Default, new LuaCompilerOptions() { SkipFirstLine = true });

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
    }
}
