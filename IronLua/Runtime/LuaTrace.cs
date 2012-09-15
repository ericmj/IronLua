using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Scripting;
using System.Linq.Expressions;
using System.Reflection;

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
            public FunctionCall(SourceSpan prefixLocation, SourceSpan callLocation)
                : this(prefixLocation, null, callLocation)
            {   }

            public FunctionCall(SourceSpan prefixLocation, string methodName, SourceSpan callLocation)
            {
                NameLocation = prefixLocation;
                MethodName = methodName;
                CallLocation = callLocation;
            }

            public string MethodName { get; private set; }
            public SourceSpan NameLocation { get; private set; }
            public SourceSpan CallLocation { get; private set; }
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
    }
}
