using System;
using System.IO;
using System.Text;
using Microsoft.Scripting;
using Microsoft.Scripting.Hosting;
using NUnit.Framework;
using System.Linq;

namespace IronLua.Tests
{
    public static class TestUtils
    {
        //public static readonly string ProjectBasePath = @"F:\workspace\DLR\IronLua-github";
        //public static readonly string ProjectBasePath = @"W:\dlr\IronLua-github";

        public static string ProjectBasePath
        {
            get
            {
                var path = TestContext.CurrentContext.TestDirectory;
                var temp = path.Split('\\').Reverse().Skip(3).Reverse();
                string o = "";
                foreach (var t in temp)
                    o += t + "\\";
                return o.Remove(o.Length - 1);
            }
        }

        public static string GetTestPath(string path)
        {
            return Path.Combine(ProjectBasePath, path);
        }

        public static T OpenReaderOrIgnoreTest<T>(Func<T> getReader)
            where T : TextReader
        {
            try
            {
                return getReader();
            }
            catch (DirectoryNotFoundException)
            {
                Assert.Ignore("Directory not found");
                return default(T);
            }
            catch (FileNotFoundException)
            {
                Assert.Ignore("File not found");
                return default(T);
            }
        }

        public static string Repeat(this char c, int n)
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

        public static string Repeat(this string s, int n)
        {
            if (n <= 0)
                return String.Empty;

            var sb = new StringBuilder(n * s.Length);
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
                Console.WriteLine("Error {1,-9} : {0}^", Repeat('=', ex.Column - 1), ex.ErrorCode);

                throw; // so test can fail
            }
        }

        public static dynamic CaptureOutput(this ScriptEngine engine, Func<ScriptEngine, dynamic> action, out string outStr, out string errStr)
        {
            var io = engine.Runtime.IO;

            // Override the output stream to capture the test output
            Stream outputStream = new MemoryStream();
            io.SetOutput(outputStream, Encoding.ASCII);

            // Override the error output stream to capture the errors
            Stream errorStream = new MemoryStream();
            io.SetErrorOutput(errorStream, Encoding.ASCII);

            try
            {
                return action(engine);
            }
            finally
            {
                // Ensure the two writers' buffer has been flushed to the MemoryStream
                io.OutputWriter.Flush();
                io.ErrorWriter.Flush();

                // Restore the console streams, disconnects the streams from ScriptIO
                io.RedirectToConsole();

                // Reset the output stream and extract the text
                outputStream.Seek(0, SeekOrigin.Begin);
                outStr = new StreamReader(outputStream, Encoding.ASCII).ReadToEnd();

                // Reset the error stream and extract the text
                errorStream.Seek(0, SeekOrigin.Begin);
                errStr = new StreamReader(errorStream, Encoding.ASCII).ReadToEnd();
            }            
        }

        public static dynamic ExecuteTestCode(this ScriptEngine engine, string code, out string outStr, out string errStr)
        {
            return CaptureOutput(engine, e => e.Execute(code), out outStr, out errStr);
        }

        public static dynamic ExecuteTestCode(this ScriptEngine engine, string code, ScriptScope scope, out string outStr, out string errStr)
        {
            return CaptureOutput(engine, e => e.Execute(code, scope), out outStr, out errStr);
        }

        public static Exception WithMessage(this Exception self, string expectedMessage)
        {
            Assert.That(self.Message, Is.EqualTo(expectedMessage));
            return self;
        }
    }

    static class StringBuilderHelpers
    {
        public static StringBuilder AppendFormatLine(this StringBuilder sb, string format, params object[] args)
        {
            return sb.AppendFormat(format, args).AppendLine();
        }
    }
}