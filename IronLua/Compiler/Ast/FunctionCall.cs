using System;
using LinqExpression = System.Linq.Expressions.Expression;

namespace IronLua.Compiler.Ast
{
    abstract class FunctionCall : Node
    {
        public class Normal : FunctionCall
        {
            public PrefixExpression Prefix { get; set; }
            public Arguments Arguments { get; set; }

            public Normal(PrefixExpression prefix, Arguments arguments)
            {
                Prefix = prefix;
                Arguments = arguments;
            }

            public override LinqExpression Compile(Scope scope)
            {
                throw new NotImplementedException();
            }
        }

        public class Table : FunctionCall
        {
            public PrefixExpression Prefix { get; set; }
            public string Name { get; set; }
            public Arguments Arguments { get; set; }

            public Table(PrefixExpression prefix, string name, Arguments arguments)
            {
                Prefix = prefix;
                Name = name;
                Arguments = arguments;
            }

            public override LinqExpression Compile(Scope scope)
            {
                throw new NotImplementedException();
            }
        }
    }
}