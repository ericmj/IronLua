using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using ExprType = System.Linq.Expressions.ExpressionType;

namespace IronLua.Runtime.Binder
{
    class BinderCache
    {
        Context context;
        Dictionary<ExprType, LuaBinaryOperationBinder> binaryOperationBinders;
        Dictionary<ExprType, LuaUnaryOperationBinder> unaryOperationBinders;
        Dictionary<InvokeMemberBinderKey, LuaInvokeMemberBinder> invokeMemberBinders;
        Dictionary<CallInfo, LuaInvokeBinder> invokeBinders;
        Dictionary<Type, LuaConvertBinder> convertBinders;
        Dictionary<string, LuaSetMemberBinder> setMemberBinders;
        LuaSetIndexBinder setIndexBinder;
        Dictionary<string, LuaGetMemberBinder> getMemberBinders;
        Dictionary<CallInfo, LuaGetIndexBinder> getIndexBinders;

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
            getIndexBinders = new Dictionary<CallInfo, LuaGetIndexBinder>();
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
            return GetCachedBinder(invokeBinders, callInfo, k => new LuaInvokeBinder(k));
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
            return setIndexBinder ?? (setIndexBinder = new LuaSetIndexBinder());
        }

        public GetMemberBinder GetGetMemberBinder(string name)
        {
            return GetCachedBinder(getMemberBinders, name, k => new LuaGetMemberBinder(k));
        }

        public GetIndexBinder GetGetIndexBinder(CallInfo info)
        {
            return GetCachedBinder(getIndexBinders, info, k => new LuaGetIndexBinder(k));
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
