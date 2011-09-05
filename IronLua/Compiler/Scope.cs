using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using Expr = System.Linq.Expressions.Expression;
using ParamExpr = System.Linq.Expressions.ParameterExpression;

namespace IronLua.Compiler
{
    class Scope
    {
        Scope parent;
        Dictionary<string, ParamExpr> variables;
        LabelTarget breakLabel;

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
            ParamExpr param;
            if (!variables.TryGetValue(name, out param))
                variables.Add(name, param = Expr.Variable(typeof(object)));

            return param;
        }

        public LabelTarget BreakLabel()
        {
            return breakLabel ?? (breakLabel = Expr.Label());
        }

        public static Scope CreateRoot()
        {
            return new Scope();
        }

        public static Scope CreateChild(Scope parent)
        {
            return new Scope
                       {
                           parent = parent,
                           breakLabel = parent.breakLabel
                       };
        }

        public static Scope CreateFunctionChild(Scope parent)
        {
            return new Scope {parent = parent};
        }
    }
}
