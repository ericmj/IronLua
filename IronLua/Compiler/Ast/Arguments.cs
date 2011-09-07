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
            public Expression.Table Value { get; set; }

            public Table(Expression.Table value)
            {
                Value = value;
            }

            public override T Visit<T>(IArgumentsVisitor<T> visitor)
            {
                return visitor.Visit(this);
            }
        }

        public class String : Arguments
        {
            public Expression.String Literal { get; set; }

            public String(Expression.String literal)
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