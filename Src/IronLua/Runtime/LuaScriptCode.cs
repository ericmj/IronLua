using System;
using System.Diagnostics.Contracts;
using System.Dynamic;
using System.Linq.Expressions;
using Microsoft.Scripting;
using Microsoft.Scripting.Runtime;

namespace IronLua.Runtime
{
    /// <summary>
    /// This class represents compiled Lua code for the language implementation
    /// support the DLR Hosting APIs require.  The DLR Hosting APIs call on
    /// this class to run code in a new ScriptScope (represented as Scope at 
    /// the language implementation level or a provided ScriptScope.    
    /// </summary>
    internal class LuaScriptCode : ScriptCode
    {
        private readonly Expression<Func<IDynamicMetaObjectProvider, dynamic>> _exprLambda;
        private Func<IDynamicMetaObjectProvider, dynamic> _compiledLambda;

        public LuaScriptCode(SourceUnit sourceUnit, Expression<Func<IDynamicMetaObjectProvider, dynamic>> chunk)
            : base(sourceUnit)
        {
            Contract.Requires(chunk != null);
            _exprLambda = chunk;
        }

        public override object Run(Scope scope)
        {
            if (_compiledLambda == null)
                _compiledLambda = _exprLambda.Compile();

            return _compiledLambda(scope);
        }
    }
}