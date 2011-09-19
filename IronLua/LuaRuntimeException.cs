using System;
using System.Runtime.Serialization;

namespace IronLua
{
    [Serializable]
    public class LuaRuntimeException : LuaException
    {
        public LuaRuntimeException(string message = null, Exception inner = null)
            : base(message, inner)
        {
        }

        public LuaRuntimeException(string format, params object[] args)
            : base(String.Format(format, args))
        {
        }

        public LuaRuntimeException(Exception inner, string format, params object[] args)
            : base(String.Format(format, args), inner)
        {
        }
        
        protected LuaRuntimeException(
            SerializationInfo info,
            StreamingContext context) : base(info, context)
        {
        }
    }
}
