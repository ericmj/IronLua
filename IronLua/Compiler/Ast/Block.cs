using System;
using System.Collections.Generic;
using LinqExpression = System.Linq.Expressions.Expression;

namespace IronLua.Compiler.Ast
{
    class Block : Node
    {
        public List<Statement> Statements { get; set; }
        public LastStatement LastStatement { get; set; }

        public Block(List<Statement> statements, LastStatement lastStatement)
        {
            Statements = statements;
            LastStatement = lastStatement;
        }

        public override LinqExpression Compile(Scope scope)
        {
            throw new NotImplementedException();
        }
    }
}