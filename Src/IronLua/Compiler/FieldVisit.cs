using Expr = System.Linq.Expressions.Expression;

namespace IronLua.Compiler
{
    class FieldVisit
    {
        public FieldVisitType Type { get; private set; }
        public Expr Member { get; private set; }
        public Expr Value { get; private set; }

        private FieldVisit()
        {
        }

        public static FieldVisit CreateImplicit(Expr value)
        {
            return new FieldVisit
                       {
                           Type = FieldVisitType.Implicit,
                           Value = value
                       };
        }

        public static FieldVisit CreateExplicit(Expr member, Expr value)
        {
            return new FieldVisit
                       {
                           Type = FieldVisitType.Explicit,
                           Member = member,
                           Value = value
                       };
        }
    }

    enum FieldVisitType
    {
        Implicit,
        Explicit
    }
}