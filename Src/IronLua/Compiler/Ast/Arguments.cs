using System.Collections.Generic;
using System.Diagnostics.Contracts;
using Microsoft.Scripting;

namespace IronLua.Compiler.Ast
{
    abstract class Arguments : Node
    {
        public SourceSpan Span { get; private set; }

        public abstract T Visit<T>(IArgumentsVisitor<T> visitor);

        public class Normal : Arguments
        {
            public List<Expression> Arguments { get; private set; }

            public Normal(List<Expression> arguments, SourceSpan span)
            {
                Contract.Requires(arguments != null);
                Arguments = arguments;
                Span = span;
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
                Span = value.Span;
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
                Span = literal.Span;
            }

            public override T Visit<T>(IArgumentsVisitor<T> visitor)
            {
                return visitor.Visit(this);
            }
        }
    }
}