using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

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
