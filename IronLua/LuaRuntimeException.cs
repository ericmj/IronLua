using System;
using System.Runtime.Serialization;
using IronLua.Runtime;
using Microsoft.Scripting;
using System.Collections;
using System.Collections.Generic;

namespace IronLua
{
    [Serializable]
    public class LuaRuntimeException : LuaException
    {
        public LuaRuntimeException(LuaContext context, string message = null, Exception inner = null)
            : base(message, inner)
        {
            Context = context;
        }

        public LuaRuntimeException(LuaContext context, string format, params object[] args)
            : base(String.Format(format, args))
        {
            Context = context;
        }
        
        protected LuaRuntimeException(SerializationInfo info, StreamingContext context) 
            : base(info, context)
        {
        }

        public LuaContext Context
        { get; private set; }

        /// <summary>
        /// Gets the currently executing block of code
        /// </summary>
        public SourceSpan CurrentBlock
        { get { return Context == null ? SourceSpan.Invalid : Context.Trace.CurrentSpan; } }

        public string GetCurrentCode()
        {
            if (!CurrentBlock.IsValid)
                return "invalid location";
            return string.Format("line {0}, column {1}", CurrentBlock.Start.Line, CurrentBlock.Start.Column);
        }


        /// <summary>
        /// Gets the actual lines of code that are currently being executed
        /// </summary>
        /// <param name="source">
        /// The source unit representing the code that is being executed
        /// </param>
        /// <returns>
        /// Returns the code that is being executed
        /// </returns>
        public string GetCurrentCode(SourceUnit source)
        {
            return GetSourceLines(source, CurrentBlock);
        }


        /// <summary>
        /// Gets the actual lines of code that are currently being executed
        /// </summary>
        /// <param name="source">
        /// The code that is being executed
        /// </param>
        /// <returns>
        /// Returns the code that is being executed
        /// </returns>
        public string GetCurrentCode(string source)
        {
            if (!CurrentBlock.IsValid)
                return "invalid location";
            return source.Substring(CurrentBlock.Start.Index, CurrentBlock.Length);
        }

        private string GetSourceLines(SourceUnit source, SourceSpan span)
        {
            if (!span.IsValid)
                return "invalid location";

            string[] codeLines = source.GetCodeLines(span.Start.Line, span.End.Line - span.Start.Line);
            string code = "";
            foreach (var line in codeLines)
                code += line + "\n";
            return code.Trim();
        }

        private string GetSourceLine(SourceUnit source, SourceSpan span)
        {
            return source.GetCodeLine(span.Start.Line);
        }

        private string GetSourceCode(string source, SourceSpan span)
        {
            if (!span.IsValid)
                return "invalid location";

            char[] buffer = new char[span.Length];

            return source.Substring(span.Start.Index, span.Length);
        }

        private string GetSourceCode(SourceUnit source, SourceSpan span)
        {
            if (!span.IsValid)
                return "invalid location";

            char[] buffer = new char[span.Length];

            using (var reader = source.GetReader())
            {
                reader.Read(buffer, span.Start.Index, buffer.Length);
                return new string(buffer);
            }
        }

        /// <summary>
        /// Gets the stack trace representing the current function call stack
        /// </summary>
        public string GetStackTrace()
        {
            LuaTrace.FunctionCall[] stack = new LuaTrace.FunctionCall[Context.Trace.CallStack.Count];
            Context.Trace.CallStack.CopyTo(stack, 0);

            string trace = "";
            for (int i = 0; i < stack.Length; i++)
                trace += (stack[i].MethodName ?? string.Format("(line {0}, column {1})", stack[i].NameLocation.Start.Line, stack[i].NameLocation.Start.Column)) + "\n";

            return trace;
        }


        /// <summary>
        /// Gets the stack trace representing the current function call stack
        /// </summary>
        /// <param name="source">The source code to allow function names to be retreived</param>
        public string GetStackTrace(string source)
        {
            LuaTrace.FunctionCall[] stack = new LuaTrace.FunctionCall[Context.Trace.CallStack.Count];
            Context.Trace.CallStack.CopyTo(stack, 0);

            string trace = "";
            for (int i = 0; i < stack.Length; i++)
                trace += (stack[i].MethodName ?? GetSourceCode(source, stack[i].NameLocation)) + "\n";

            return trace;
        }

        /// <summary>
        /// Gets the stack trace representing the current function call stack
        /// </summary>
        /// <param name="source">The source code to allow function names to be retreived</param>
        public string GetStackTrace(SourceUnit source)
        {
            LuaTrace.FunctionCall[] stack = new LuaTrace.FunctionCall[Context.Trace.CallStack.Count];
            Context.Trace.CallStack.CopyTo(stack, 0);
            
            string trace = "";
            for (int i = 0; i < stack.Length; i++)            
                trace += (stack[i].MethodName ?? GetSourceCode(source, stack[i].NameLocation)) + "\n";
            
            return trace;
        }
    }
}
