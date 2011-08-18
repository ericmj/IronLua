using System.Collections.Generic;

namespace IronLua.Compiler.Ast
{
    abstract class Arguments : Node
    {
        public abstract T Visit<T>(IArgumentsVisitor<T> visitor);

        public class Normal : Arguments
        {
            public List<Expression> Arguments { get; set; }

            public Normal(List<Expression> arguments)
            {
                Arguments = arguments;
            }

            public override T Visit<T>(IArgumentsVisitor<T> visitor)
            {
                return visitor.Visit(this);
            }
        }

        public class Table : Arguments
        {
            public List<Field> Fields { get; set; }

            public Table(List<Field> fields)
            {
                Fields = fields;
            }

            public override T Visit<T>(IArgumentsVisitor<T> visitor)
            {
                return visitor.Visit(this);
            }
        }

        public class String : Arguments
        {
            public string Literal { get; set; }

            public String(string literal)
            {
                Literal = literal;
            }

            public override T Visit<T>(IArgumentsVisitor<T> visitor)
            {
                return visitor.Visit(this);
            }
        }
    }
}