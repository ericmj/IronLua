using System;
using LinqExpression = System.Linq.Expressions.Expression;

namespace IronLua.Compiler.Ast
{
    class Block : Node
    {
        public Statement[] Statements { get; private set; }
        public LastStatement LastStatement { get; private set; }

        public Block(Statement[] statements, LastStatement lastStatement)
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