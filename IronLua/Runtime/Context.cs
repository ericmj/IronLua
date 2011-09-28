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

        internal BaseLibrary BaseLibrary;
        internal StringLibrary StringLibrary;

        Func<object, object, object> getDynamicIndexCache;
        Func<object, object, object, object> getDynamicNewIndexCache;
        Func<Delegate, object> getDynamicCallCache0;
        Func<Delegate, object, object> getDynamicCallCache1;

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
            BaseLibrary = new BaseLibrary(this);
            StringLibrary = new StringLibrary(this);

            Globals = new LuaTable();
            BaseLibrary.Setup(Globals);
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

        internal Func<Delegate, object> GetDynamicCall0()
        {
            if (getDynamicCallCache0 != null)
                return getDynamicCallCache0;

            var funcVar = Expr.Parameter(typeof(object));
            var expr = Expr.Lambda<Func<Delegate, object>>(
                Expr.Dynamic(BinderCache.GetInvokeBinder(new CallInfo(0)), typeof(object), funcVar), funcVar);

            return getDynamicCallCache0 = expr.Compile();
        }

        internal Func<Delegate, object, object> GetDynamicCall1()
        {
            if (getDynamicCallCache1 != null)
                return getDynamicCallCache1;

            var funcVar = Expr.Parameter(typeof(Delegate));
            var argVar = Expr.Parameter(typeof(object));
            var expr = Expr.Lambda<Func<Delegate, object, object>>(
                Expr.Dynamic(BinderCache.GetInvokeBinder(new CallInfo(1)), typeof(object), funcVar, argVar),
                funcVar, argVar);

            return getDynamicCallCache1 = expr.Compile();
        }
    }
}
