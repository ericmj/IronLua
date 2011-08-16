using System;
using LinqExpression = System.Linq.Expressions.Expression;

namespace IronLua.Compiler.Ast
{
    class FunctionBody : Node
    {
        public string[] Parameters { get; private set; }
        public bool Varargs { get; private set; }
        public Block Body { get; private set; }

        public FunctionBody(string[] parameters, bool varargs, Block body)
        {
            Parameters = parameters;
            Varargs = varargs;
            Body = body;
        }

        public override LinqExpression Compile(Scope scope)
        {
            throw new NotImplementedException();
        }
    }
}
