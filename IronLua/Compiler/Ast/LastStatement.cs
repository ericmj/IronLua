using System.Collections.Generic;

namespace IronLua.Compiler.Ast
{
    abstract class LastStatement : Node
    {
        public abstract T Visit<T>(ILastStatementVisitor<T> visitor);

        public class Return : LastStatement
        {
            public List<Expression> Values { get; set; }

            public Return(List<Expression> values)
            {
                Values = values;
            }

            public override T Visit<T>(ILastStatementVisitor<T> visitor)
            {
                return visitor.Visit(this);
            }
        }

        public class Break : LastStatement
        {

            public override T Visit<T>(ILastStatementVisitor<T> visitor)
            {
                return visitor.Visit(this);
            }
        }
    }
}