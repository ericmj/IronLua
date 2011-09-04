using System.Collections.Generic;
using Expr = System.Linq.Expressions.Expression;
using ParamExpr = System.Linq.Expressions.ParameterExpression;

namespace IronLua.Compiler
{
    class Scope
    {
        Scope parent;
        Dictionary<string, ParamExpr> variables;

        public bool IsRoot { get { return parent == null; } }

        private Scope()
        {
            variables = new Dictionary<string, ParamExpr>();
        }

        public ParamExpr[] AllLocals()
        {
            var values = variables.Values;
            var array = new ParamExpr[values.Count];
            values.CopyTo(array, 0);
            return array;
        }

        public ParamExpr FindIdentifier(string name)
        {
            ParamExpr param;
            if (variables.TryGetValue(name, out param))
                return param;
            if (parent != null)
                return parent.FindIdentifier(name);

            return null;
        }

        public ParamExpr AddLocal(string name)
        {
            var param = Expr.Variable(typeof(object));
            variables.Add(name, param);
            return param;
        }

        public static Scope CreateRoot()
        {
            return new Scope();
        }

        public static Scope CreateChild(Scope parent)
        {
            return new Scope {parent = parent};
        }
    }
}
