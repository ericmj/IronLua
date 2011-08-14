namespace IronLua.Compiler.Ast
{
    class Field : Node
    {
        public class MemberExpr : Field
        {
            public Expression Member { get; private set; }
            public Expression Value { get; private set; }

            public MemberExpr(Expression member, Expression value)
            {
                Member = member;
                Value = value;
            }
        }

        public class MemberId : Field
        {
            public string Member { get; private set; }
            public Expression Value { get; private set; }

            public MemberId(string member, Expression value)
            {
                Member = member;
                Value = value;
            }
        }

        public class Normal : Field
        {
            public Expression Value { get; private set; }

            public Normal(Expression value)
            {
                Value = value;
            }
        }
    }
}