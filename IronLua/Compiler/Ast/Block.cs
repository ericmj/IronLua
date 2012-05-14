using System.Collections.Generic;
using System.Diagnostics.Contracts;

namespace IronLua.Compiler.Ast
{
    class Block : Node
    {
        public List<Statement> Statements { get; private set; }

        public Block(List<Statement> statements)
        {
            Contract.Requires(statements != null);
            Contract.Requires(statements.Count > 0);
            Statements = statements;
        }

        public Block(Statement statement)
            : this(new List<Statement>() { statement })
        {
        }
    }
}