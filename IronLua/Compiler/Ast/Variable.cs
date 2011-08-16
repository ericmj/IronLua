using System;
using LinqExpression = System.Linq.Expressions.Expression;

namespace IronLua.Compiler.Ast
{
    abstract class Variable : Node
    {
        public class Identifier : Variable
        {
            public string Value { get; set; }

            public Identifier(string value)
            {
                Value = value;
            }

            public override LinqExpression Compile(Scope scope)
            {
                throw new NotImplementedException();
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

            public override LinqExpression Compile(Scope scope)
            {
                throw new NotImplementedException();
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

            public override LinqExpression Compile(Scope scope)
            {
                throw new NotImplementedException();
            }
        }
    }
}
