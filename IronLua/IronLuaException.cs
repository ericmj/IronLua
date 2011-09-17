using System;
using System.Runtime.Serialization;

namespace IronLua
{
    [Serializable]
    public class IronLuaException : Exception
    {
        public IronLuaException(string message = null, Exception inner = null) : base(message, inner)
        {
        }

        protected IronLuaException(
            SerializationInfo info,
            StreamingContext context) : base(info, context)
        {
        }
    }
}
