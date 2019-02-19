using System;
using System.Runtime.Serialization;

namespace IronLua
{
    [Serializable]
    public class LuaException : Exception
    {
        public LuaException(string message = null, Exception inner = null)
            : base(message, inner)
        {
        }

        protected LuaException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
