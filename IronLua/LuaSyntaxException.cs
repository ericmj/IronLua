using System;
using System.Runtime.Serialization;
using IronLua.Compiler.Parser;

namespace IronLua
{
    [Serializable]
    public class LuaSyntaxException : LuaException
    {
        public string File { get; private set; }
        public int Line { get; private set; }
        public int Column { get; private set; }

        public LuaSyntaxException(string message = null, Exception inner = null)
            : base(message, inner)
        {
        }

        public LuaSyntaxException(string file, int line, int column, string message, Exception inner = null)
            : base(message, inner)
        {
            File = file;
            Line = line;
            Column = column;
        }

        internal LuaSyntaxException(Input input, string message, Exception inner = null)
            : this(input.File, input.Line, input.Column, message, inner)
        {
        }

        internal LuaSyntaxException(Input input, string format, params object[] args)
            : this(input, String.Format(format, args))
        {
        }

        internal LuaSyntaxException(Input input, Exception inner, string format, params object[] args)
            : this(input, String.Format(format, args), inner)
        {
        }

        protected LuaSyntaxException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
