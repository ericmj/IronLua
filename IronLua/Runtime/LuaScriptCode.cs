using System;
using System.Linq.Expressions;
using Microsoft.Scripting;
using Microsoft.Scripting.Runtime;

namespace IronLua.Runtime
{
    internal class LuaScriptCode : ScriptCode
    {
        public LuaScriptCode(SourceUnit sourceUnit)
            : base(sourceUnit)
        {
        }

        private readonly Func<dynamic> _code;

        public LuaScriptCode(SourceUnit sourceUnit, Func<dynamic> chunk)
            : base(sourceUnit)
        {
            _code = chunk;
        }

        public override object Run(Scope scope)
        {
            //Console.WriteLine("This is where we 'execute' the compiled code");            

            if (_code != null)
            {
                return _code();
            }
            else
            {
                throw new NotImplementedException();
            }
        }
    }
}