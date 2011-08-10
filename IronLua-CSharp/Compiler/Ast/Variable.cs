namespace IronLua_CSharp.Compiler.Ast
{
    class Variable : Node
    {
        public class Name : Variable
        {
            public string Identifier { get; private set; }

            public Name(string identifier)
            {
                Identifier = identifier;
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