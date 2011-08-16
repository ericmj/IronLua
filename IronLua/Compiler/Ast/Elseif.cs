using System;
using LinqExpression = System.Linq.Expressions.Expression;

namespace IronLua.Compiler.Ast
{
    class Elseif : Node
    {
        public Expression Test { get; private set; }
        public Block Body { get; private set; }

        public Elseif(Expression test, Block body)
        {
            Test = test;
            Body = body;
        }

        public override LinqExpression Compile(Scope scope)
        {
            throw new NotImplementedException();
        }
    }
}