using System.Collections.Generic;
using System.Linq.Expressions;

namespace IronLua.Compiler
{
    class Scope
    {
        Scope parent;
        Dictionary<string, ParameterExpression> variables;
        Dictionary<string, ParameterExpression> globals;

        public Scope()
        {
            parent = null;
            variables = new Dictionary<string, ParameterExpression>();
            globals = new Dictionary<string, ParameterExpression>();
        }

        public Scope(Scope parent)
        {
            this.parent = parent;
            variables = new Dictionary<string, ParameterExpression>();
            globals = parent.globals;
        }

        public ParameterExpression FindIdentifier(string name)
        {
            ParameterExpression param;
            if (variables.TryGetValue(name, out param))
                return param;
            if (parent != null)
                return parent.FindIdentifier(name);
            if (globals.TryGetValue(name, out param))
                return param;

            return null;
        }

        public ParameterExpression AddLocal(string name)
        {
            var param = Expression.Variable(typeof(object));
            variables.Add(name, param);
            return param;
        }

        public ParameterExpression AddGlobal(string name)
        {
            var param = Expression.Variable(typeof(object));
            globals.Add(name, param);
            return param;
        }
    }
}
