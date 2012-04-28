using System.Collections.Generic;

namespace IronLua.Compiler.Ast
{
    class Block : Node
    {
        public List<Statement> Statements { get; set; }

        public Block(List<Statement> statements)
        {
            Statements = statements;
        }
    }
}