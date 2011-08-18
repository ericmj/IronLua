namespace IronLua.Compiler.Ast
{
    abstract class PrefixExpression : Node
    {
        public abstract T Visit<T>(IPrefixExpressionVisitor<T> visitor);

        public class Variable : PrefixExpression
        {
            public Ast.Variable Var { get; set; }

            public Variable(Ast.Variable variable)
            {
                Var = variable;
            }

            public override T Visit<T>(IPrefixExpressionVisitor<T> visitor)
            {
                return visitor.Visit(this);
            }
        }

        public class FunctionCall : PrefixExpression
        {
            public Ast.FunctionCall Call { get; set; }

            public FunctionCall(Ast.FunctionCall call)
            {
                Call = call;
            }

            public override T Visit<T>(IPrefixExpressionVisitor<T> visitor)
            {
                return visitor.Visit(this);
            }
        }

        public class Expression : PrefixExpression
        {
            public Ast.Expression Expr { get; set; }

            public Expression(Ast.Expression expression)
            {
                Expr = expression;
            }

            public override T Visit<T>(IPrefixExpressionVisitor<T> visitor)
            {
                return visitor.Visit(this);
            }
        }
    }
}