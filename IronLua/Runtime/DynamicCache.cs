using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq.Expressions;
using IronLua.Runtime.Binder;

namespace IronLua.Runtime
{
    // TODO: Make thread-safe
    class DynamicCache
    {
        readonly Context context;
        readonly Dictionary<ExpressionType, LuaBinaryOperationBinder> binaryOperationBinders;
        readonly Dictionary<ExpressionType, LuaUnaryOperationBinder> unaryOperationBinders;
        readonly Dictionary<InvokeMemberBinderKey, LuaInvokeMemberBinder> invokeMemberBinders;
        readonly Dictionary<CallInfo, LuaInvokeBinder> invokeBinders;
        readonly Dictionary<Type, LuaConvertBinder> convertBinders;
        readonly Dictionary<string, LuaSetMemberBinder> setMemberBinders;
        readonly Dictionary<string, LuaGetMemberBinder> getMemberBinders;
        LuaSetIndexBinder setIndexBinder;
        LuaGetIndexBinder getIndexBinder;

        Func<object, object, object> getDynamicIndexCache;
        Func<object, object, object, object> getDynamicNewIndexCache;
        Func<object, object> getDynamicCallCache0;
        Func<object, object, object> getDynamicCallCache1;
        Func<object, object, object, object> getDynamicCallCache2;
        Func<object, object, object, object, object> getDynamicCallCache3;

        public DynamicCache(Context context)
        {
            this.context = context;
            binaryOperationBinders = new Dictionary<ExpressionType, LuaBinaryOperationBinder>();
            unaryOperationBinders = new Dictionary<ExpressionType, LuaUnaryOperationBinder>();
            invokeMemberBinders = new Dictionary<InvokeMemberBinderKey, LuaInvokeMemberBinder>();
            invokeBinders = new Dictionary<CallInfo, LuaInvokeBinder>();
            convertBinders = new Dictionary<Type, LuaConvertBinder>();
            setMemberBinders = new Dictionary<string, LuaSetMemberBinder>();
            getMemberBinders = new Dictionary<string, LuaGetMemberBinder>();
        }

        public BinaryOperationBinder GetBinaryOperationBinder(ExpressionType operation)
        {
            return GetCachedBinder(binaryOperationBinders, operation, k => new LuaBinaryOperationBinder(context, k));
        }

        public UnaryOperationBinder GetUnaryOperationBinder(ExpressionType operation)
        {
            return GetCachedBinder(unaryOperationBinders, operation, k => new LuaUnaryOperationBinder(context, k));
        }

        public InvokeMemberBinder GetInvokeMemberBinder(string name, CallInfo info)
        {
            return GetCachedBinder(invokeMemberBinders, new InvokeMemberBinderKey(name, info),
                                   k => new LuaInvokeMemberBinder(context, k.Name, k.Info));
        }

        public InvokeBinder GetInvokeBinder(CallInfo callInfo)
        {
            return GetCachedBinder(invokeBinders, callInfo, k => new LuaInvokeBinder(context, k));
        }

        public ConvertBinder GetConvertBinder(Type type)
        {
            return GetCachedBinder(convertBinders, type, k => new LuaConvertBinder(k));
        }

        public SetMemberBinder GetSetMemberBinder(string name)
        {
            return GetCachedBinder(setMemberBinders, name, k => new LuaSetMemberBinder(k));
        }

        public SetIndexBinder GetSetIndexBinder()
        {
            return setIndexBinder ?? (setIndexBinder = new LuaSetIndexBinder(context));
        }

        public GetMemberBinder GetGetMemberBinder(string name)
        {
            return GetCachedBinder(getMemberBinders, name, k => new LuaGetMemberBinder(k));
        }

        public GetIndexBinder GetGetIndexBinder()
        {
            return getIndexBinder ?? (getIndexBinder = new LuaGetIndexBinder(context));
        }

        TValue GetCachedBinder<TKey, TValue>(Dictionary<TKey, TValue> cache, TKey key, Func<TKey, TValue> newer)
        {
            TValue binder;
            if (cache.TryGetValue(key, out binder))
                return binder;
            return cache[key] = newer(key);
        }

        // Stolen from DLR's reference implementation Sympl
        class InvokeMemberBinderKey
        {
            public string Name { get; private set; }
            public CallInfo Info { get; private set; }

            public InvokeMemberBinderKey(string name, CallInfo info)
            {
                Name = name;
                Info = info;
            }

            public override bool Equals(object obj)
            {
                var key = obj as InvokeMemberBinderKey;
                return key != null && Name == key.Name && Info.Equals(key.Info);
            }

            public override int GetHashCode()
            {
                return 0x28000000 ^ Name.GetHashCode() ^ Info.GetHashCode();
            }
        }

        public Func<object, object, object> GetDynamicIndex()
        {
            if (getDynamicIndexCache != null)
                return getDynamicIndexCache;

            var objVar = Expression.Parameter(typeof(object));
            var keyVar = Expression.Parameter(typeof(object));
            var expr = Expression.Lambda<Func<object, object, object>>(
                Expression.Dynamic(Context.DynamicCache.GetGetIndexBinder(), typeof(object), objVar, keyVar),
                objVar, keyVar);

            return getDynamicIndexCache = expr.Compile();
        }

        public Func<object, object, object, object> GetDynamicNewIndex()
        {
            if (getDynamicNewIndexCache != null)
                return getDynamicNewIndexCache;

            var objVar = Expression.Parameter(typeof(object));
            var keyVar = Expression.Parameter(typeof(object));
            var valueVar = Expression.Parameter(typeof(object));
            var expr = Expression.Lambda<Func<object, object, object, object>>(
                Expression.Dynamic(Context.DynamicCache.GetSetIndexBinder(), typeof(object), objVar, keyVar, valueVar),
                objVar, keyVar, valueVar);

            return getDynamicNewIndexCache = expr.Compile();
        }

        public Func<object, object> GetDynamicCall0()
        {
            if (getDynamicCallCache0 != null)
                return getDynamicCallCache0;

            var funcVar = Expression.Parameter(typeof(object));
            var expr = Expression.Lambda<Func<object, object>>(
                Expression.Dynamic(Context.DynamicCache.GetInvokeBinder(new CallInfo(0)), typeof(object), funcVar), funcVar);

            return getDynamicCallCache0 = expr.Compile();
        }

        public Func<object, object, object> GetDynamicCall1()
        {
            if (getDynamicCallCache1 != null)
                return getDynamicCallCache1;

            var funcVar = Expression.Parameter(typeof(object));
            var argVar = Expression.Parameter(typeof(object));
            var expr = Expression.Lambda<Func<object, object, object>>(
                Expression.Dynamic(Context.DynamicCache.GetInvokeBinder(new CallInfo(1)), typeof(object), funcVar, argVar),
                funcVar, argVar);

            return getDynamicCallCache1 = expr.Compile();
        }

        public Func<object, object, object, object> GetDynamicCall2()
        {
            if (getDynamicCallCache2 != null)
                return getDynamicCallCache2;

            var funcVar = Expression.Parameter(typeof(object));
            var arg1Var = Expression.Parameter(typeof(object));
            var arg2Var = Expression.Parameter(typeof(object));

            var expr = Expression.Lambda<Func<object, object, object, object>>(
                Expression.Dynamic(Context.DynamicCache.GetInvokeBinder(new CallInfo(2)), typeof(object), funcVar, arg1Var, arg2Var),
                funcVar, arg1Var, arg2Var);

            return getDynamicCallCache2 = expr.Compile();
        }

        public Func<object, object, object, object, object> GetDynamicCall3()
        {
            if (getDynamicCallCache3 != null)
                return getDynamicCallCache3;

            var funcVar = Expression.Parameter(typeof(object));
            var arg1Var = Expression.Parameter(typeof(object));
            var arg2Var = Expression.Parameter(typeof(object));
            var arg3Var = Expression.Parameter(typeof(object));

            var expr = Expression.Lambda<Func<object, object, object, object, object>>(
                Expression.Dynamic(Context.DynamicCache.GetInvokeBinder(new CallInfo(3)), typeof(object), funcVar, arg1Var, arg2Var, arg3Var),
                funcVar, arg1Var, arg2Var, arg3Var);

            return getDynamicCallCache3 = expr.Compile();
        }
    }
}
