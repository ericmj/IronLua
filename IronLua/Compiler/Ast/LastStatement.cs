using System;
using System.Collections.Generic;
using Expr = System.Linq.Expressions.Expression;

namespace IronLua.Compiler.Ast
{
    abstract class LastStatement : Node
    {
        public class Return : LastStatement
        {
            public List<Expression> Values { get; set; }

            public Return(List<Expression> values)
            {
                Values = values;
            }

            public override Expr Compile(Scope scope)
            {
                throw new NotImplementedException();
            }
        }

        public class Break : LastStatement
        {

            public override Expr Compile(Scope scope)
            {
                throw new NotImplementedException();
            }
        }
    }
}