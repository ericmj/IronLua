namespace IronLua_CSharp.Compiler.Ast
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
    }
}