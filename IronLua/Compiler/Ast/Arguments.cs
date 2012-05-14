using System.Collections.Generic;
using System.Diagnostics.Contracts;

namespace IronLua.Compiler.Ast
{
    abstract class Arguments : Node
    {
        public abstract T Visit<T>(IArgumentsVisitor<T> visitor);

        public class Normal : Arguments
        {
            public List<Expression> Arguments { get; private set; }

            public Normal(List<Expression> arguments)
            {
                Contract.Requires(arguments != null);
                Arguments = arguments;
            }

            public override T Visit<T>(IArgumentsVisitor<T> visitor)
            {
                return visitor.Visit(this);
            }
        }

        public class Table : Arguments
        {
            public Expression.Table Value { get; private set; }

            public Table(Expression.Table value)
            {
                Contract.Requires(value != null);
                Value = value;
            }

            public override T Visit<T>(IArgumentsVisitor<T> visitor)
            {
                return visitor.Visit(this);
            }
        }

        public class String : Arguments
        {
            public Expression.String Literal { get; private set; }

            public String(Expression.String literal)
            {
                Contract.Requires(literal != null);
                Literal = literal;
            }

            public override T Visit<T>(IArgumentsVisitor<T> visitor)
            {
                return visitor.Visit(this);
            }
        }
    }
}