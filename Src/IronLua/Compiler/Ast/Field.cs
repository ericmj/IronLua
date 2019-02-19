namespace IronLua.Compiler.Ast
{
    abstract class Field : Node
    {
        public abstract T Visit<T>(IFieldVisitor<T> visitor);

        public class MemberExpr : Field
        {
            public Expression Member { get; set; }
            public Expression Value { get; set; }

            public MemberExpr(Expression member, Expression value)
            {
                Member = member;
                Value = value;
            }

            public override T Visit<T>(IFieldVisitor<T> visitor)
            {
                return visitor.Visit(this);
            }
        }

        public class MemberId : Field
        {
            public string Member { get; set; }
            public Expression Value { get; set; }

            public MemberId(string member, Expression value)
            {
                Member = member;
                Value = value;
            }

            public override T Visit<T>(IFieldVisitor<T> visitor)
            {
                return visitor.Visit(this);
            }
        }

        public class Normal : Field
        {
            public Expression Value { get; set; }

            public Normal(Expression value)
            {
                Value = value;
            }

            public override T Visit<T>(IFieldVisitor<T> visitor)
            {
                return visitor.Visit(this);
            }
        }
    }
}