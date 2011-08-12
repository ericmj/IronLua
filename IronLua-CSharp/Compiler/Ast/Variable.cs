namespace IronLua_CSharp.Compiler.Ast
{
    class Variable : Node
    {
        public class Identifier : Variable
        {
            public string Value { get; private set; }

            public Identifier(string value)
            {
                Value = value;
            }
        }

        public class MemberExpr : Variable
        {
            public PrefixExpression Prefix { get; private set; }
            public Expression Member { get; private set; }

            public MemberExpr(PrefixExpression prefix, Expression member)
            {
                Prefix = prefix;
                Member = member;
            }
        }

        public class MemberId : Variable
        {
            public PrefixExpression Prefix { get; private set; }
            public string Member { get; private set; }

            public MemberId(PrefixExpression prefix, string member)
            {
                Prefix = prefix;
                Member = member;
            }
        }
    }
}