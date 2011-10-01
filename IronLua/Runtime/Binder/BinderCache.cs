using System;
using System.Collections.Generic;
using System.Dynamic;
using ExprType = System.Linq.Expressions.ExpressionType;

namespace IronLua.Runtime.Binder
{
    // TODO: Make thread-safe
    class BinderCache
    {
        readonly Context context;
        readonly Dictionary<ExprType, LuaBinaryOperationBinder> binaryOperationBinders;
        readonly Dictionary<ExprType, LuaUnaryOperationBinder> unaryOperationBinders;
        readonly Dictionary<InvokeMemberBinderKey, LuaInvokeMemberBinder> invokeMemberBinders;
        readonly Dictionary<CallInfo, LuaInvokeBinder> invokeBinders;
        readonly Dictionary<Type, LuaConvertBinder> convertBinders;
        readonly Dictionary<string, LuaSetMemberBinder> setMemberBinders;
        readonly Dictionary<string, LuaGetMemberBinder> getMemberBinders;
        LuaSetIndexBinder setIndexBinder;
        LuaGetIndexBinder getIndexBinder;

        public BinderCache(Context context)
        {
            this.context = context;
            binaryOperationBinders = new Dictionary<ExprType, LuaBinaryOperationBinder>();
            unaryOperationBinders = new Dictionary<ExprType, LuaUnaryOperationBinder>();
            invokeMemberBinders = new Dictionary<InvokeMemberBinderKey, LuaInvokeMemberBinder>();
            invokeBinders = new Dictionary<CallInfo, LuaInvokeBinder>();
            convertBinders = new Dictionary<Type, LuaConvertBinder>();
            setMemberBinders = new Dictionary<string, LuaSetMemberBinder>();
            getMemberBinders = new Dictionary<string, LuaGetMemberBinder>();
        }

        public BinaryOperationBinder GetBinaryOperationBinder(ExprType operation)
        {
            return GetCachedBinder(binaryOperationBinders, operation, k => new LuaBinaryOperationBinder(context, k));
        }

        public UnaryOperationBinder GetUnaryOperationBinder(ExprType operation)
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
    }
}
