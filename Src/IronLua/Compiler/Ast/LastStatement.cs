using System.Collections.Generic;

namespace IronLua.Compiler.Ast
{
    abstract class LastStatement : Statement
    {
        public class Return : LastStatement
        {
            public List<Expression> Values { get; set; }

            public Return(List<Expression> values)
            {
                Values = values;
            }

            public override T Visit<T>(IStatementVisitor<T> visitor)
            {
                return visitor.Visit(this);
            }
        }

        public class Break : LastStatement
        {
            public override T Visit<T>(IStatementVisitor<T> visitor)
            {
                return visitor.Visit(this);
            }
        }
    }
}