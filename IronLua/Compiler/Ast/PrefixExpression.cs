namespace IronLua.Compiler.Ast
{
    abstract class PrefixExpression : Node
    {
        public class Variable : PrefixExpression
        {
            public Ast.Variable Var { get; private set; }

            public Variable(Ast.Variable variable)
            {
                Var = variable;
            }
        }

        public class FunctionCall : PrefixExpression
        {
            public Ast.FunctionCall Call { get; private set; }

            public FunctionCall(Ast.FunctionCall call)
            {
                Call = call;
            }
        }

        public class Expression : PrefixExpression
        {
            public Ast.Expression Expr { get; private set; }

            public Expression(Ast.Expression expression)
            {
                Expr = expression;
            }
        }
    }
}