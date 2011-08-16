using System;
using LinqExpression = System.Linq.Expressions.Expression;

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

            public override LinqExpression Compile(Scope scope)
            {
                throw new NotImplementedException();
            }
        }

        public class FunctionCall : PrefixExpression
        {
            public Ast.FunctionCall Call { get; private set; }

            public FunctionCall(Ast.FunctionCall call)
            {
                Call = call;
            }

            public override LinqExpression Compile(Scope scope)
            {
                throw new NotImplementedException();
            }
        }

        public class Expression : PrefixExpression
        {
            public Ast.Expression Expr { get; private set; }

            public Expression(Ast.Expression expression)
            {
                Expr = expression;
            }

            public override LinqExpression Compile(Scope scope)
            {
                throw new NotImplementedException();
            }
        }
    }
}