using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using IronLua.Library;
using IronLua.Runtime.Binder;
using Expr = System.Linq.Expressions.Expression;
using ExprType = System.Linq.Expressions.ExpressionType;
using ParamExpr = System.Linq.Expressions.ParameterExpression;

namespace IronLua.Runtime
{
    class Context
    {
        public LuaTable Globals { get; private set; }
        public BinderCache BinderCache { get; private set; }
        public Dictionary<Type, LuaTable> Metatables { get; private set; }

        internal Global GlobalLibrary;
        internal LuaString StringLibrary;

        public Context()
        {
            BinderCache = new BinderCache(this);

            SetupLibraries();

            Metatables =
                new Dictionary<Type, LuaTable>
                    {
                        {typeof(bool), new LuaTable()},
                        {typeof(double), new LuaTable()},
                        {typeof(string), new LuaTable()},
                        {typeof(Delegate), new LuaTable()},
                    };
        }

        void SetupLibraries()
        {
            GlobalLibrary = new Global(this);
            StringLibrary = new LuaString(this);

            Globals = new LuaTable();
            GlobalLibrary.Setup(Globals);
            //StringLibrary.Setup(StringGlobals);
        }

        internal object GetMetamethod(string methodName, object obj)
        {
            LuaTable metatable;
            if (!Metatables.TryGetValue(obj.GetType(), out metatable))
                throw new Exception(); // TODO

            return metatable.GetValue(methodName);
        }
    }
}
