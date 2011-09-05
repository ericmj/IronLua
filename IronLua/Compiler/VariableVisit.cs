using System.Linq.Expressions;

namespace IronLua.Compiler
{
    class VariableVisit
    {
        public VariableType Type { get; private set; }
        public Expression Object { get; private set; }
        public Expression Index { get; private set; }

        public VariableVisit(VariableType type, Expression @object, Expression index)
        {
            Type = type;
            Object = @object;
            Index = index;
        }
    }

    enum VariableType
    {
        MemberId,
        MemberExpr
    }
}