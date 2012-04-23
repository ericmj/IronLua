using IronLua.Compiler;
using IronLua.Compiler.Parser;
using IronLua.Hosting;
using IronLua.Runtime;
using Microsoft.Scripting;
using NUnit.Framework;

namespace IronLua.Tests.Compiler
{
    // Note: See ParserTests class for some documentation

    [TestFixture]
    public class GeneratorTests
    {

        public void GeneratorTest(SourceUnit sourceUnit, bool useLua52)
        {            
            var options = new LuaCompilerOptions()
            {
                SkipFirstLine = true,
                UseLua52Features = useLua52,
            };

            var reader = ParserTests.OpenReaderOrIgnoreTest(sourceUnit.GetReader);

            ParserTests.AssertSyntaxError(() =>
            {
                var tokenizer = new Tokenizer(ErrorSink.Default, options);
                tokenizer.Initialize(null, reader, sourceUnit, SourceLocation.MinValue);
                var parser = new Parser(tokenizer, tokenizer.ErrorSink, options);
                var ast = parser.Parse();
                Assert.That(ast, Is.Not.Null);
                var gen = new Generator(((LuaContext)sourceUnit.LanguageContext).Ctx);
                var expr = gen.Compile(ast);
                Assert.That(expr, Is.Not.Null);
            });
        }

        public void GeneratorSnippetTest(string luaCode)
        {
            var useLua52 = true;

            var engine = Lua.CreateEngine();
            var context = Lua.GetLuaContext(engine);
            var sourceUnit = context.CreateSnippet(luaCode, SourceCodeKind.Expression);

            GeneratorTest(sourceUnit, useLua52);
        }

        public void GeneratorFileTest(string luaFile, bool useLua52)
        {
            var engine = Lua.CreateEngine();
            var context = Lua.GetLuaContext(engine);
            var sourceUnit = context.CreateFileUnit(luaFile);

            GeneratorTest(sourceUnit, useLua52);
        }


        [Test, TestCaseSource(typeof(ParserTests.LuaTestSuiteSource), "Lua52TestCases")]
        public void GeneratorTestOnLua52TestSuite(string luaFile)
        {
            GeneratorFileTest(luaFile, useLua52: true);
        }

        [Test, TestCaseSource(typeof(ParserTests.LuaTestSuiteSource), "Lua51TestCases")]
        public void GeneratorTestOnLua51TestSuite(string luaFile)
        {
            GeneratorFileTest(luaFile, useLua52: false);
        }

        [Test]
        public void GenAndTest01()
        {
            GeneratorSnippetTest("return x and y");
        }
    }
}
