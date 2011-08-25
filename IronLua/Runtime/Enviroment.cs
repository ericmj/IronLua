using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using IronLua.Library;
using IronLua.Runtime.Binder;
using Expr = System.Linq.Expressions.Expression;
using ParamExpr = System.Linq.Expressions.ParameterExpression;

namespace IronLua.Runtime
{
    class Enviroment
    {
        public LuaTable Globals { get; private set; }

        public BinderCache BinderCache { get; private set; }
        public ParamExpr GlobalsExpr { get; private set; }

        public Enviroment()
        {
            BinderCache = new BinderCache(this);
            GlobalsExpr = Expr.Parameter(typeof(LuaTable), "Globals");
            Globals = new LuaTable();
            Global.Setup(Globals);
        }
    }
}
