using System;
using Microsoft.Scripting;

namespace IronLua.Compiler
{
    [Serializable]
    public class LuaCompilerOptions : CompilerOptions
    {
        public static readonly LuaCompilerOptions Default = new LuaCompilerOptions();

        public bool SkipFirstLine { get; set; }
        public bool MultiEolns { get; set; }
        public bool UseLua51Features { get; private set; }
        public bool UseLua52Features { get; set; }
        public int InitialBufferCapacity { get; set; }

        public LuaCompilerOptions()
        {
            SkipFirstLine = true;
            MultiEolns = true;
            UseLua51Features = true;
            UseLua52Features = true;
            InitialBufferCapacity = 1024;
        }
    }
}