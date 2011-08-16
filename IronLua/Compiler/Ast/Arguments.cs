using System;
using System.Collections.Generic;
using Expr = System.Linq.Expressions.Expression;

namespace IronLua.Compiler.Ast
{
    abstract class Arguments : Node
    {
        public class Normal : Arguments
        {
            public List<Expression> Arguments { get; set; }

            public Normal(List<Expression> arguments)
            {
                Arguments = arguments;
            }

            public override Expr Compile(Scope scope)
            {
                throw new NotImplementedException();
            }
        }

        public class Table : Arguments
        {
            public List<Field> Fields { get; set; }

            public Table(List<Field> fields)
            {
                Fields = fields;
            }

            public override Expr Compile(Scope scope)
            {
                throw new NotImplementedException();
            }
        }

        public class String : Arguments
        {
            public string Literal { get; set; }

            public String(string literal)
            {
                Literal = literal;
            }

            public override Expr Compile(Scope scope)
            {
                throw new NotImplementedException();
            }
        }
    }
}