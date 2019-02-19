using Microsoft.Scripting;

namespace IronLua.Compiler.Ast
{
    abstract class PrefixExpression : Node
    {
        public SourceSpan Span { get; private set; }

        public abstract T Visit<T>(IPrefixExpressionVisitor<T> visitor);

        public class Variable : PrefixExpression
        {
            public Ast.Variable Var { get; set; }

            public Variable(Ast.Variable variable)
            {
                Var = variable;
                Span = variable.Span;
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
                Span = call.Span;
            }

            public override T Visit<T>(IPrefixExpressionVisitor<T> visitor)
            {
                return visitor.Visit(this);
            }
        }

        public class Expression : PrefixExpression
        {
            public Ast.Expression Expr { get; set; }

            public Expression(Ast.Expression expression, SourceSpan span)
            {
                Expr = expression;
                Span = span;
            }

            public Expression(Ast.Expression expression)
                : this(expression, expression.Span)
            {
            }

            public override T Visit<T>(IPrefixExpressionVisitor<T> visitor)
            {
                return visitor.Visit(this);
            }
        }
    }
}