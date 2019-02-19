using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Scripting;
using System.Linq.Expressions;
using System.Reflection;
using System.Dynamic;
using IronLua.Compiler;

namespace IronLua.Runtime
{
    class LuaTrace
    {
        LuaContext _context;

        public LuaTrace(LuaContext context)
        {
            _context = context;
            CallStack = new Stack<FunctionCall>();
        }

        #region Current Source Span

        public SourceSpan CurrentSpan
        { get; private set; }

        public void UpdateSourceSpan(SourceSpan span)
        {
            CurrentSpan = span;
        }

        private static readonly MethodInfo UpdateSourceSpanMethodInfo = typeof(LuaTrace).GetMethod("UpdateSourceSpan");
        public static Expression MakeUpdateSourceSpan(LuaContext context, SourceSpan span)
        {
            return Expression.Call(Expression.Constant(context.Trace), UpdateSourceSpanMethodInfo, Expression.Constant(span));
        }

        #endregion

        #region Function Call Stack

        public class FunctionCall
        {
            public FunctionCall(SourceSpan functionLocation, FunctionType type, string identifier, SymbolDocumentInfo document = null)
            {
                FunctionLocation = functionLocation;
                Type = type;
                Document = document ?? Expression.SymbolDocument("[CLR]");
                MethodName = identifier;
            }

            public FunctionCall(SourceSpan functionLocation, FunctionType type, IEnumerable<string> identifiers, SymbolDocumentInfo document = null)
            {
                FunctionLocation = functionLocation;
                Type = type;
                Document = document ?? Expression.SymbolDocument("[CLR]");

                string temp = identifiers.First();
                foreach (var i in identifiers.Skip(1))
                    temp += "." + i;

                MethodName = temp;
            }

            public SymbolDocumentInfo Document { get; private set; }
            public string MethodName { get; private set; }
            public SourceSpan FunctionLocation { get; private set; }
            public FunctionType Type { get; private set; }
        }

        public enum FunctionType
        {
            Lua,
            CLR,
            Chunk
        }

        public Stack<FunctionCall> CallStack
        { get; private set; }

        private static readonly MethodInfo PushCallStackMethodInfo = typeof(Stack<FunctionCall>).GetMethod("Push");
        private static readonly MethodInfo PopCallStackMethodInfo = typeof(Stack<FunctionCall>).GetMethod("Pop");

        public static Expression MakePushFunctionCall(LuaContext context, FunctionCall call)
        {
            return Expression.Call(Expression.Constant(context.Trace.CallStack), PushCallStackMethodInfo, Expression.Constant(call));
        }

        public static Expression MakePopFunctionCall(LuaContext context)
        {
            return Expression.Call(Expression.Constant(context.Trace.CallStack), PopCallStackMethodInfo);
        }

        #endregion

        #region Current Scope

        public IDynamicMetaObjectProvider CurrentScopeStorage
        { get; private set; }

        public void UpdateCurrentScopeStorage(IDynamicMetaObjectProvider scopeStorage)
        {
            CurrentScopeStorage = scopeStorage;
        }

        public LuaScope CurrentEvaluationScope
        { get; private set; }

        public void UpdateCurrentEvaluationScope(LuaScope scope)
        {
            CurrentEvaluationScope = scope;
        }


        private static readonly MethodInfo UpdateCurrentEvaluationScopeMethodInfo = typeof(LuaTrace).GetMethod("UpdateCurrentEvaluationScope");
        private static readonly MethodInfo UpdateScopeStorageMethodInfo = typeof(LuaTrace).GetMethod("UpdateCurrentScopeStorage");
        public static Expression MakeUpdateCurrentEvaluationScope(LuaContext context, LuaScope scope)
        {
            return Expression.Block(
                Expression.Call(Expression.Constant(context.Trace), UpdateCurrentEvaluationScopeMethodInfo, Expression.Constant(scope)),
                Expression.Call(Expression.Constant(context.Trace), UpdateScopeStorageMethodInfo, scope.GetDlrGlobals()));
        }

        #endregion
    }
}
