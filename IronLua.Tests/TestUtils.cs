using System;
using System.IO;
using System.Text;
using Microsoft.Scripting;
using NUnit.Framework;

namespace IronLua.Tests
{
    public static class TestUtils
    {
        //public static readonly string ProjectBasePath = @"F:\workspace\DLR\IronLua-github";
        public static readonly string ProjectBasePath = @"W:\dlr\IronLua-github";

        public static string GetTestPath(string path)
        {
            return Path.Combine(ProjectBasePath, path);
        }

        public static StreamReader SafeOpenText(string f)
        {
            try
            {
                return File.OpenText(f);
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
    }
}