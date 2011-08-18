using System.Collections.Generic;
using System.Linq.Expressions;

namespace IronLua.Compiler
{
    class Scope
    {
        Scope parent;
        Dictionary<string, ParameterExpression> variables;

        public bool IsRoot { get { return parent == null; } }

        private Scope()
        {
            variables = new Dictionary<string, ParameterExpression>();
        }

        public ParameterExpression FindIdentifier(string name)
        {
            ParameterExpression param;
            if (variables.TryGetValue(name, out param))
                return param;
            if (parent != null)
                return parent.FindIdentifier(name);

            return null;
        }

        public ParameterExpression AddLocal(string name)
        {
            var param = Expression.Variable(typeof(object));
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
