namespace IronLua.Compiler.Ast
{
    class LastStatement : Node
    {
        public class Return : LastStatement
        {
            public Expression[] Values { get; private set; }

            public Return(Expression[] values)
            {
                Values = values;
            }
        }

        public class Break : LastStatement
        {
        }
    }
}