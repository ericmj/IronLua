using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using IronLua.Compiler;
using IronLua.Compiler.Parser;
using Microsoft.Scripting;
using Microsoft.Scripting.Runtime;
using Microsoft.Scripting.Utils;

namespace IronLua.Runtime
{
    public sealed class LuaContext : LanguageContext
    {
        readonly Context _ctx = new Context();

        public LuaContext(ScriptDomainManager manager, IDictionary<string, object> options = null)
            : base(manager)
        {
            // TODO: options
        }

        public override ScriptCode CompileSourceCode(SourceUnit sourceUnit, CompilerOptions options, ErrorSink errorSink)
        {
            ContractUtils.RequiresNotNull(sourceUnit, "sourceUnit");
            ContractUtils.RequiresNotNull(options, "options");
            ContractUtils.RequiresNotNull(errorSink, "errorSink");
            ContractUtils.Requires(sourceUnit.LanguageContext == this, "Language mismatch.");

            Console.WriteLine("This is where we 'compile' the source code");

            var sw = Stopwatch.StartNew();
            try
            {
                var source = sourceUnit.GetCode();
                var input = new Input(source);
                var parser = new Parser(input);
                var ast = parser.Parse();
                var gen = new Generator(_ctx);
                var expr = gen.Compile(ast);
                var chunk = expr.Compile();
                return new LuaScriptCode(sourceUnit, chunk);
            }
            finally
            {
                Debug.Print("Parse of '{0}' took {1} ms", sourceUnit, sw.ElapsedMilliseconds);                
            }

            throw new NotImplementedException();
        }

        #region Lua Information

        private static readonly Guid LuaLanguageGuid = new Guid("03ed4b80-d10b-442f-ad9a-47dae85b2051");

        private static readonly Lazy<Version> LuaLanguageVersion = new Lazy<Version>(GetLuaVersion);

        public override Guid LanguageGuid
        {
            get { return LuaLanguageGuid; }
        }

        public override Version LanguageVersion
        {
            get { return LuaLanguageVersion.Value; }
        }

        internal static Version GetLuaVersion()
        {
            return new AssemblyName(typeof(LuaContext).Assembly.FullName).Version;
        }

        #endregion


    }
}