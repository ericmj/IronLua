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

        public static LuaContext LuaContext { get; private set; }

        internal BaseLibrary BaseLibrary;
        internal StringLibrary StringLibrary;
        internal MathLibrary MathLibrary;
        internal OSLibrary OSLibrary;

        internal static DynamicCache DynamicCache { get; private set; }

        public Context(LuaContext luaContext)
        {
            LuaContext = luaContext;

            DynamicCache = new DynamicCache(this.LuaContext);

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
            Globals = new LuaTable();

            BaseLibrary = new BaseLibrary(this);
            BaseLibrary.Setup(Globals);

            //TableLibrary = new TableLibrary();
            var tablelibTable = new LuaTable();
            //TableLibrary.Setup(tablelibTable);
            Globals.SetValue("table", tablelibTable);

            MathLibrary = new MathLibrary(this);
            var mathlibTable = new LuaTable();
            MathLibrary.Setup(mathlibTable);
            Globals.SetValue("math", mathlibTable);

            StringLibrary = new StringLibrary(this);
            var strlibTable = new LuaTable();
            StringLibrary.Setup(strlibTable);
            Globals.SetValue("string", strlibTable);

            //IoLibrary = new IoLibrary(this);
            var iolibTable = new LuaTable();
            //IoLibrary.Setup(iolibTable);
            Globals.SetValue("io", iolibTable);

            OSLibrary = new OSLibrary(this);
            var oslibTable = new LuaTable();
            OSLibrary.Setup(oslibTable);
            Globals.SetValue("os", oslibTable);

            //DebugLibrary = new DebugLibrary(this);
            var debuglibTable = new LuaTable();
            //DebugLibrary.Setup(debuglibTable);
            Globals.SetValue("debug", debuglibTable);
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
