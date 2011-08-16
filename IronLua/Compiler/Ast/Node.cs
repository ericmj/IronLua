using LinqExpression = System.Linq.Expressions.Expression;

namespace IronLua.Compiler.Ast
{
    abstract class Node
    {
        public abstract LinqExpression Compile(Scope scope);
    }
}