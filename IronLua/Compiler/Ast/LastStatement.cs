using System;
using LinqExpression = System.Linq.Expressions.Expression;

namespace IronLua.Compiler.Ast
{
    abstract class LastStatement : Node
    {
        public class Return : LastStatement
        {
            public Expression[] Values { get; private set; }

            public Return(Expression[] values)
            {
                Values = values;
            }

            public override LinqExpression Compile(Scope scope)
            {
                throw new NotImplementedException();
            }
        }

        public class Break : LastStatement
        {

            public override LinqExpression Compile(Scope scope)
            {
                throw new NotImplementedException();
            }
        }
    }
}