using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading;
using IronLua.Compiler;
using IronLua.Compiler.Parser;
using IronLua.Hosting;
using IronLua.Runtime.Binder;
using Microsoft.Scripting;
using Microsoft.Scripting.Hosting;
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

            _binder = new LuaBinder(this);
        }

        internal Context Ctx
        {
            get { return _ctx; }
        }

        public override ScriptCode CompileSourceCode(SourceUnit sourceUnit, CompilerOptions options, ErrorSink errorSink)
        {
            ContractUtils.RequiresNotNull(sourceUnit, "sourceUnit");
            ContractUtils.RequiresNotNull(options, "options");
            ContractUtils.RequiresNotNull(errorSink, "errorSink");
            ContractUtils.Requires(sourceUnit.LanguageContext == this, "Language mismatch.");

            //Console.WriteLine("This is where we 'compile' the source code");

            var luaOptions = options as LuaCompilerOptions;
            if (luaOptions == null)
                throw new ArgumentException("Compiler context required", "options");
            
            SourceCodeReader reader;
            try
            {
                reader = sourceUnit.GetReader();

                if (luaOptions.SkipFirstLine)
                    reader.ReadLine();
            }
            catch (IOException ex)
            {
                errorSink.Add(sourceUnit, ex.Message, SourceSpan.Invalid, 0, Severity.Error);
                throw;
            }

            using (reader)
            {
#if false
                var source = reader.ReadToEnd();
                var input = new Input(source);
                var lexer = new Lexer(input);
#else
                var lexer = new Tokenizer(errorSink, luaOptions);
                lexer.Initialize(null, reader, sourceUnit, SourceLocation.MinValue);
#endif
                var parser = new Parser(lexer, errorSink);
                var ast = parser.Parse();
                var gen = new Generator(_ctx);
                var expr = gen.Compile(ast);
                var lamda = expr.Compile();

                //sourceUnit.CodeProperties = ScriptCodeParseResult.Complete;
                return new LuaScriptCode(sourceUnit, lamda);
            }
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

        Lazy<LuaCompilerOptions> _compilerOptions = 
            new Lazy<LuaCompilerOptions>(() => new LuaCompilerOptions());

        public override CompilerOptions GetCompilerOptions()
        {
            return _compilerOptions.Value;
        }

        public override TService GetService<TService>(params object[] args)            
        {
            if (typeof(TService) == typeof(TokenizerService))
            {
                return (TService)(object)new Tokenizer(ErrorSink.Null, (LuaCompilerOptions)GetCompilerOptions());
            }
            else if (typeof(TService) == typeof(LuaService))
            {
                return (TService)(object)GetLuaService((ScriptEngine)args[0]);
            }
            else
            {
                return base.GetService<TService>(args);
            }
        }

        #region LuaService

        LuaService _luaService;

        internal LuaService GetLuaService(ScriptEngine engine)
        {
            if (_luaService == null)
            {
                var service = new LuaService(this, engine);
                Interlocked.CompareExchange(ref _luaService, service, null);
            }
            return _luaService;
        }

        #endregion

        readonly LuaBinder _binder;
        internal LuaBinder Binder
        {
            get { return _binder; }
        }

    }
}
