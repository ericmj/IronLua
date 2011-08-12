using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace IronLua_CSharp
{
    public class CompileException : IronLuaException
    {
        public string File { get; private set; }
        public int Line { get; private set; }
        public int Column { get; private set; }

        public CompileException(string message = null, Exception inner = null) : base(message, inner)
        {
        }

        public CompileException(string file, int line, int column, string message, Exception inner = null)
            : base(message, inner)
        {
            File = file;
            Line = line;
            Column = column;
        }

        internal CompileException(Compiler.Input input, string message, Exception inner = null)
            : this(input.File, input.Line, input.Column, message, inner)
        {
        }

        internal CompileException(Compiler.Input input, string format, params object[] args)
            : this(input, String.Format(format, args))
        {
        }

        internal CompileException(Compiler.Input input, Exception inner, string format, params object[] args)
            : this(input, String.Format(format, args), inner)
        {
        }

        protected CompileException(
            SerializationInfo info,
            StreamingContext context) : base(info, context)
        {
        }
    }
}
