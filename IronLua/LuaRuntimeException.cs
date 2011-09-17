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
        
        protected LuaRuntimeException(
            SerializationInfo info,
            StreamingContext context) : base(info, context)
        {
        }
    }
}
