namespace IronLua_CSharp.Compiler.Ast
{
    class Arguments : Node
    {
        public class Normal : Arguments
        {
            public Expression[] Arguments { get; private set; }

            public Normal(Expression[] arguments)
            {
                Arguments = arguments;
            }
        }

        public class Table : Arguments
        {
            public Field[] Fields { get; private set; }

            public Table(Field[] fields)
            {
                Fields = fields;
            }
        }

        public class String : Arguments
        {
            public string Literal { get; private set; }

            public String(string literal)
            {
                Literal = literal;
            }
        }
    }
}