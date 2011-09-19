using System;
using System.Collections.Generic;
using System.Dynamic;
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

        Func<object, object, object> getDynamicIndexCache;
        Func<object, object, object, object> getDynamicNewIndexCache;
        Func<Delegate, object, object, object> getDynamicCallCache;

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

        internal LuaTable GetMetatable(object obj)
        {
            if (obj == null)
                return null;

            LuaTable table;
            if ((table = obj as LuaTable) != null)
                return table.Metatable;

            if (Metatables.TryGetValue(obj.GetType(), out table))
                return table;

            throw new ArgumentOutOfRangeException("obj", "Argument is of non-supported type");
        }

        internal object GetMetamethod(object obj, string methodName)
        {
            var metatable = GetMetatable(obj);
            return methodName == null || metatable == null ? null : metatable.GetValue(methodName);
        }

        internal Func<object, object, object> GetDynamicIndex()
        {
            if (getDynamicIndexCache != null)
                return getDynamicIndexCache;

            var objVar = Expr.Parameter(typeof(object));
            var keyVar = Expr.Parameter(typeof(object));
            var expr = Expr.Lambda<Func<object, object, object>>(
                Expr.Dynamic(BinderCache.GetGetIndexBinder(), typeof(object), objVar, keyVar),
                objVar, keyVar);

            return getDynamicIndexCache = expr.Compile();
        }

        internal Func<object, object, object, object> GetDynamicNewIndex()
        {
            if (getDynamicNewIndexCache != null)
                return getDynamicNewIndexCache;

            var objVar = Expr.Parameter(typeof(object));
            var keyVar = Expr.Parameter(typeof(object));
            var valueVar = Expr.Parameter(typeof(object));
            var expr = Expr.Lambda<Func<object, object, object, object>>(
                Expr.Dynamic(BinderCache.GetSetIndexBinder(), typeof(object), objVar, keyVar, valueVar),
                objVar, keyVar, valueVar);

            return getDynamicNewIndexCache = expr.Compile();
        }

        // Only works for 2 arguments
        internal Func<Delegate, object, object, object> GetDynamicCall2()
        {
            if (getDynamicCallCache != null)
                return getDynamicCallCache;

            var funcVar = Expr.Parameter(typeof(object));
            var arg1Var = Expr.Parameter(typeof(object));
            var arg2Var = Expr.Parameter(typeof(object));
            var expr = Expr.Lambda<Func<object, object, object, object>>(
                Expr.Dynamic(BinderCache.GetInvokeBinder(new CallInfo(2)), typeof(object), funcVar, arg1Var, arg2Var),
                funcVar, arg1Var, arg2Var);

            return getDynamicCallCache = expr.Compile();
        }
    }
}
