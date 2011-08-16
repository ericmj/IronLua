using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using IronLua.Util;
using Expr = System.Linq.Expressions.Expression;

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

        public override Expr Compile(Scope scope)
        {
            scope = new Scope(scope);

            var linqStatements = Statements.Select(s => s.Compile(scope));

            if (LastStatement != null)
                linqStatements = linqStatements.Add(LastStatement.Compile(scope));

            return Expr.Block(linqStatements);
        }
    }
}