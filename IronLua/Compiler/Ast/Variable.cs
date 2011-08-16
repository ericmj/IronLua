using System;
using LinqExpression = System.Linq.Expressions.Expression;

namespace IronLua.Compiler.Ast
{
    abstract class Variable : Node
    {
        public class Identifier : Variable
        {
            public string Value { get; private set; }

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
            public PrefixExpression Prefix { get; private set; }
            public Expression Member { get; private set; }

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
            public PrefixExpression Prefix { get; private set; }
            public string Member { get; private set; }

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
