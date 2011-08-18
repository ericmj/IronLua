namespace IronLua.Compiler.Ast
{
    abstract class FunctionCall : Node
    {
        public abstract T Visit<T>(IFunctionCallVisitor<T> visitor);

        public class Normal : FunctionCall
        {
            public PrefixExpression Prefix { get; set; }
            public Arguments Arguments { get; set; }

            public Normal(PrefixExpression prefix, Arguments arguments)
            {
                Prefix = prefix;
                Arguments = arguments;
            }

            public override T Visit<T>(IFunctionCallVisitor<T> visitor)
            {
                return visitor.Visit(this);
            }
        }

        public class Table : FunctionCall
        {
            public PrefixExpression Prefix { get; set; }
            public string Name { get; set; }
            public Arguments Arguments { get; set; }

            public Table(PrefixExpression prefix, string name, Arguments arguments)
            {
                Prefix = prefix;
                Name = name;
                Arguments = arguments;
            }

            public override T Visit<T>(IFunctionCallVisitor<T> visitor)
            {
                return visitor.Visit(this);
            }
        }
    }
}