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
    }
}