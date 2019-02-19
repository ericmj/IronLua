using Microsoft.Scripting;

namespace IronLua.Compiler.Ast
{
    abstract class Variable : Node
    {
        public SourceSpan Span;

        public abstract T Visit<T>(IVariableVisitor<T> visitor);

        public class Identifier : Variable
        {
            public string Value { get; set; }

            public Identifier(string value)
            {
                Value = value;
            }

            public override T Visit<T>(IVariableVisitor<T> visitor)
            {
                return visitor.Visit(this);
            }
        }

        public class MemberExpr : Variable
        {
            public PrefixExpression Prefix { get; set; }
            public Expression Member { get; set; }

            public MemberExpr(PrefixExpression prefix, Expression member)
            {
                Prefix = prefix;
                Member = member;
            }

            public override T Visit<T>(IVariableVisitor<T> visitor)
            {
                return visitor.Visit(this);
            }
        }

        public class MemberId : Variable
        {
            public PrefixExpression Prefix { get; set; }
            public string Member { get; set; }

            public MemberId(PrefixExpression prefix, string member)
            {
                Prefix = prefix;
                Member = member;
            }

            public override T Visit<T>(IVariableVisitor<T> visitor)
            {
                return visitor.Visit(this);
            }
        }
    }
}
