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
        LabelTarget returnLabel;

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

        public bool TryGetLocal(string name, out ParamExpr local)
        {
            if (variables.TryGetValue(name, out local))
                return true;
            if (parent != null)
                return parent.TryGetLocal(name, out local);

            return false;
        }

        public ParamExpr AddLocal(string name)
        {
            // We have this behavior so that ex. "local x, x = 1, 2" works
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

        public LabelTarget AddReturnLabel()
        {
            return returnLabel = Expr.Label(typeof(object));
        }

        public LabelTarget GetReturnLabel()
        {
            if (returnLabel != null)
                return returnLabel;
            if (parent == null)
                return null;
            return parent.GetReturnLabel();
        }
    }
}
