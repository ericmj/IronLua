using Expr = System.Linq.Expressions.Expression;

namespace IronLua.Compiler.Ast
{
    abstract class Node
    {
        public abstract Expr Compile(Scope scope);
    }
}