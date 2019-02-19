using System;
using System.Runtime.Serialization;

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

        internal LuaSyntaxException(string file, int line, int column, string message, Exception inner = null)
            : base(message, inner)
        {
            File = file;
            Line = line;
            Column = column;
        }

        protected LuaSyntaxException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
