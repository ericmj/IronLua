using System.Collections.Generic;
using System.Diagnostics.Contracts;
using Microsoft.Scripting;

namespace IronLua.Compiler.Ast
{
    class Block : Node
    {
        public SourceSpan Span { get; set; }
        public List<Statement> Statements { get; private set; }

        public Block(List<Statement> statements)
        {
            Contract.Requires(statements != null);
            Statements = statements;
        }

        public Block(Statement statement)
            : this(new List<Statement>() { statement })
        {
            Contract.Requires(statement != null);
            Span = statement.Span;
        }
    }
}