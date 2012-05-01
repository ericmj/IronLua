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
        
        protected LuaRuntimeException(SerializationInfo info, StreamingContext context) 
            : base(info, context)
        {
        }
    }
}
