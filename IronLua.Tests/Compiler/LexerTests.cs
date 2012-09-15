using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using IronLua.Compiler;
using IronLua.Compiler.Parsing;
using IronLua.Hosting;
using Microsoft.Scripting;
using Microsoft.Scripting.Hosting;
using NUnit.Framework;

namespace IronLua.Tests
{
    // Note: See ParserTests class for some documentation

    [TestFixture]
    public class LexerTests
    {
        ScriptEngine engine;

        [TestFixtureSetUp]
        public void CreateLuaEngine()
        {
            engine = Lua.CreateEngine();
            Assert.That(engine, Is.Not.Null);
        }

        public void CheckLuaInterpeter(string luaInterp)
        {
            if (!File.Exists(luaInterp))
                Assert.Ignore("Lua interpeter not found");
        }

        string lua50 = TestUtils.GetTestPath(@"libs\lua5.0.exe");
        string lua51 = TestUtils.GetTestPath(@"libs\lua5.1.exe");
        string lua52 = TestUtils.GetTestPath(@"libs\lua5.2.exe");

        public string ExecuteLuaSnippet(string exeFile, string snippet, bool giveStdErr = false)
        {
            var s = new ProcessStartInfo
            {
                UseShellExecute = false,
                RedirectStandardInput = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                FileName = exeFile,
                Arguments = "-e \"" + snippet + "\"",
            };

            var p = Process.Start(s);

            Thread.Sleep(25);

            var buffer = new char[64];
            var stdout = new StringBuilder(1024);
            var stderr = new StringBuilder(1024);
            while (true)
            {
                int nb_bytes_read;

                do
                {
                    nb_bytes_read = p.StandardOutput.Read(buffer, 0, buffer.Length);
                    stdout.Append(buffer, 0, nb_bytes_read);
                } while (nb_bytes_read > 0);

                do
                {
                    nb_bytes_read = p.StandardError.Read(buffer, 0, buffer.Length);
                    stderr.Append(buffer, 0, nb_bytes_read);
                } while (nb_bytes_read > 0);

                if (p.HasExited)
                {
                    break;
                }

                Thread.Sleep(100);
            }

            //Console.WriteLine("ExitCode was: {0}", p.ExitCode);
            //Console.WriteLine("stdout: {0}", stdout);
            //Console.WriteLine("stderr: {0}", stderr);

            string result;
            if (giveStdErr)
            {
                result = stderr.ToString();
                result = result.Replace(s.FileName, "").TrimStart(':', ' ');
                result = result.Replace("(command line):1:", ""); // Lua 5.1, 5.2
                result = result.Replace("<command line>:1:", ""); // Lua 5.0
            }
            else
            {
                result = stdout.ToString();
            }
            return result.TrimEnd('\r', '\n').Trim();
        }

        public void LexerFailureTests(string exeFile, string snippet, string expect)
        {
            CheckLuaInterpeter(exeFile);
            bool xfail = TestContext.CurrentContext.Test.Properties.Contains("FailureCase");
            //Console.WriteLine("snipet {0}", snippet);
            string actual = ExecuteLuaSnippet(exeFile, snippet, xfail);
            //Console.WriteLine("actual {0}", actual);
            //Console.WriteLine("expect {0}", expect);
            //Console.WriteLine();
            Assert.That(actual, Is.EqualTo(expect));
        }

        //[Test, TestCaseSource(typeof(TestDataSource), "LexerTestCases")]
        public void LexerFailureTestsUnderLua50(string snippet, string expect)
        {
            LexerFailureTests(lua50, snippet, expect);
        }

        //[Test, TestCaseSource(typeof(TestDataSource), "LexerTestCases")]
        public void LexerFailureTestsUnderLua51(string snippet, string expect)
        {
            LexerFailureTests(lua51, snippet, expect);
        }

        //[Test, TestCaseSource(typeof(TestDataSource), "LexerTestCases")]
        public void LexerFailureTestsUnderLua52(string snippet, string expect)
        {
            LexerFailureTests(lua52, snippet, expect);
        }
        
        [Test, TestCaseSource(typeof(TestDataSource), "LexerTestCases")]
        public void LexerErrorReportTests(string snippet, string expect)
        {
            bool mustfail = TestContext.CurrentContext.Test.Properties.Contains("FailureCase");

            var tokenizer = new Tokenizer(ErrorSink.Default, new LuaCompilerOptions() { SkipFirstLine = true });

            var sourceUnit = engine.GetLuaContext().CreateSnippet(snippet, SourceCodeKind.Expression);

            tokenizer.Initialize(null, sourceUnit.GetReader(), sourceUnit, SourceLocation.MinValue);
            try
            {
                var unused = tokenizer.EnumerateTokens(s => true) // all tokens
                                      .TakeWhile(t => t.Symbol != Symbol.Eof)
                                      .Last();
                if (mustfail)
                    Assert.Fail("Expected a SyntaxErrorException");
            }
            catch (SyntaxErrorException ex)
            {
                Assert.That(ex.Message, Is.EqualTo(expect));
            }
        }

        public static class TestDataSource
        {
            public static TestCaseData[] LexerTestCases()
            {
                return LexerFailureCases().ToArray();
            }

            public static IEnumerable<TestCaseData> LexerFailureCases()
            {
                var f = TestUtils.GetTestPath(@"IronLua.Tests\Scripts\Lexer01_XXX.lua");

                using (var reader = File.OpenText(f))
                {
                    var snippet = new StringBuilder();
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        if (line.StartsWith("--XX") || line.StartsWith("--::"))
                        {
                            // failure cases
                            var testData = snippet.ToString();
                            var expect = line.TrimStart('-', 'X', ':').Trim();

                            var testCaseData = new TestCaseData(testData, expect);

                            if (line.StartsWith("--XX"))
                                testCaseData.SetProperty("FailureCase", 1);

                            testCaseData.SetName(testData);
                            yield return testCaseData;

                            snippet.Clear();
                        }
                        else if (!line.StartsWith("--"))
                        {
                            snippet.Append(line);
                        }
                    }
                }

            }
        }
    }
}
