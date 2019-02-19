using System;
using System.Collections.Generic;
using Microsoft.Scripting;

namespace IronLua.Runtime
{
    [Serializable]
    public class LuaOptions : LanguageOptions
    {
        private readonly bool _verbose;

        public LuaOptions(IDictionary<string, object> options)
            : base(options)
        {
            _verbose = GetOption(options, "Verbose", false);
        }

        public bool Verbose
        {
            get { return _verbose; }
        }
    }
}