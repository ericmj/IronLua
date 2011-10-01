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
        public Dictionary<Type, LuaTable> Metatables { get; private set; }

        internal BaseLibrary BaseLibrary;
        internal StringLibrary StringLibrary;

        internal static BinderCache BinderCache { get; private set; }
        // TODO: Move to BinderCache and rename it DynamicCache
        static Func<object, object, object> getDynamicIndexCache;
        static Func<object, object, object, object> getDynamicNewIndexCache;
        static Func<object, object> getDynamicCallCache0;
        static Func<object, object, object> getDynamicCallCache1;
        static Func<object, object, object, object> getDynamicCallCache2;
        static Func<object, object, object, object, object> getDynamicCallCache3;

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

        internal LuaTable GetTypeMetatable(object obj)
        {
            if (obj == null)
                return null;

            LuaTable table;
            if (Metatables.TryGetValue(obj.GetType(), out table))
                return table;

            throw new ArgumentOutOfRangeException("obj", "Argument is of non-supported type");
        }

        internal static Func<object, object, object> GetDynamicIndex()
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

        internal static Func<object, object, object, object> GetDynamicNewIndex()
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

        internal static Func<object, object> GetDynamicCall0()
        {
            if (getDynamicCallCache0 != null)
                return getDynamicCallCache0;

            var funcVar = Expr.Parameter(typeof(object));
            var expr = Expr.Lambda<Func<object, object>>(
                Expr.Dynamic(BinderCache.GetInvokeBinder(new CallInfo(0)), typeof(object), funcVar), funcVar);

            return getDynamicCallCache0 = expr.Compile();
        }

        internal static Func<object, object, object> GetDynamicCall1()
        {
            if (getDynamicCallCache1 != null)
                return getDynamicCallCache1;

            var funcVar = Expr.Parameter(typeof(object));
            var argVar = Expr.Parameter(typeof(object));
            var expr = Expr.Lambda<Func<object, object, object>>(
                Expr.Dynamic(BinderCache.GetInvokeBinder(new CallInfo(1)), typeof(object), funcVar, argVar),
                funcVar, argVar);

            return getDynamicCallCache1 = expr.Compile();
        }

        internal static Func<object, object, object, object> GetDynamicCall2()
        {
            if (getDynamicCallCache2 != null)
                return getDynamicCallCache2;

            var funcVar = Expr.Parameter(typeof(object));
            var arg1Var = Expr.Parameter(typeof(object));
            var arg2Var = Expr.Parameter(typeof(object));

            var expr = Expr.Lambda<Func<object, object, object, object>>(
                Expr.Dynamic(BinderCache.GetInvokeBinder(new CallInfo(2)), typeof(object), funcVar, arg1Var, arg2Var),
                funcVar, arg1Var, arg2Var);

            return getDynamicCallCache2 = expr.Compile();
        }

        internal static Func<object, object, object, object, object> GetDynamicCall3()
        {
            if (getDynamicCallCache3 != null)
                return getDynamicCallCache3;

            var funcVar = Expr.Parameter(typeof(object));
            var arg1Var = Expr.Parameter(typeof(object));
            var arg2Var = Expr.Parameter(typeof(object));
            var arg3Var = Expr.Parameter(typeof(object));

            var expr = Expr.Lambda<Func<object, object, object, object, object>>(
                Expr.Dynamic(BinderCache.GetInvokeBinder(new CallInfo(3)), typeof(object), funcVar, arg1Var, arg2Var, arg3Var),
                funcVar, arg1Var, arg2Var, arg3Var);

            return getDynamicCallCache3 = expr.Compile();
        }
    }
}
