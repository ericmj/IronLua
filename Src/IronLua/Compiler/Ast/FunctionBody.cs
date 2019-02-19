using System.Collections.Generic;
using Microsoft.Scripting;

namespace IronLua.Compiler.Ast
{
    class FunctionBody : Node
    {
        public SourceSpan Span;

        public List<string> Parameters { get; private set; }
        public bool HasVarargs { get; private set; }
        public Block Body { get; private set; }

        public FunctionBody(List<string> parameters, bool varargs, Block body)
        {
            Parameters = parameters;
            HasVarargs = varargs;
            Body = body;
        }
    }
}
