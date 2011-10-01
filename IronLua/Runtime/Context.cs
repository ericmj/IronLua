using System;
using System.Collections.Generic;
using IronLua.Library;
using Expr = System.Linq.Expressions.Expression;
using ExprType = System.Linq.Expressions.ExpressionType;
using ParamExpr = System.Linq.Expressions.ParameterExpression;

namespace IronLua.Runtime
{
    class Context
    {
        public LuaTable Globals { get; private set; }
        public Dictionary<Type, LuaTable> Metatables { get; private set; }

        internal BaseLibrary BaseLibrary;
        internal StringLibrary StringLibrary;

        internal static DynamicCache DynamicCache { get; private set; }

        public Context()
        {
            DynamicCache = new DynamicCache(this);

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
            BaseLibrary = new BaseLibrary(this);
            StringLibrary = new StringLibrary(this);

            Globals = new LuaTable();
            BaseLibrary.Setup(Globals);
            //StringLibrary.Setup(StringGlobals);
        }

        internal LuaTable GetTypeMetatable(object obj)
        {
            if (obj == null)
                return null;

            LuaTable table;
            if (Metatables.TryGetValue(obj.GetType(), out table))
                return table;

            throw new ArgumentOutOfRangeException("obj", "Argument is of non-supported type");
        }
    }
}
