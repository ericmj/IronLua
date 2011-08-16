using System;
using LinqExpression = System.Linq.Expressions.Expression;

namespace IronLua.Compiler.Ast
{
    abstract class Arguments : Node
    {
        public class Normal : Arguments
        {
            public Expression[] Arguments { get; private set; }

            public Normal(Expression[] arguments)
            {
                Arguments = arguments;
            }

            public override LinqExpression Compile(Scope scope)
            {
                throw new NotImplementedException();
            }
        }

        public class Table : Arguments
        {
            public Field[] Fields { get; private set; }

            public Table(Field[] fields)
            {
                Fields = fields;
            }

            public override LinqExpression Compile(Scope scope)
            {
                throw new NotImplementedException();
            }
        }

        public class String : Arguments
        {
            public string Literal { get; private set; }

            public String(string literal)
            {
                Literal = literal;
            }

            public override LinqExpression Compile(Scope scope)
            {
                throw new NotImplementedException();
            }
        }
    }
}