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

        public BinderCache(Context context)
        {
            this.context = context;
            binaryOperationBinders = new Dictionary<ExprType, LuaBinaryOperationBinder>();
            unaryOperationBinders = new Dictionary<ExprType, LuaUnaryOperationBinder>();
            invokeMemberBinders = new Dictionary<InvokeMemberBinderKey, LuaInvokeMemberBinder>();
            invokeBinders = new Dictionary<CallInfo, LuaInvokeBinder>();
            convertBinders = new Dictionary<Type, LuaConvertBinder>();
            setMemberBinders = new Dictionary<string, LuaSetMemberBinder>();
        }

        public BinaryOperationBinder GetBinaryOperationBinder(ExprType operation)
        {
            LuaBinaryOperationBinder binder;
            if (binaryOperationBinders.TryGetValue(operation, out binder))
                return binder;

            return binaryOperationBinders[operation] = new LuaBinaryOperationBinder(context, operation);
        }

        public UnaryOperationBinder GetUnaryOperationBinder(ExprType operation)
        {
            LuaUnaryOperationBinder binder;
            if (unaryOperationBinders.TryGetValue(operation, out binder))
                return binder;

            return unaryOperationBinders[operation] = new LuaUnaryOperationBinder(context, operation);
        }

        public InvokeMemberBinder GetInvokeMemberBinder(string name, CallInfo info)
        {
            var key = new InvokeMemberBinderKey(name, info);
            LuaInvokeMemberBinder binder;
            if (invokeMemberBinders.TryGetValue(key, out binder))
                return binder;

            return invokeMemberBinders[key] = new LuaInvokeMemberBinder(context, name, info);
        }

        public InvokeBinder GetInvokeBinder(CallInfo callInfo)
        {
            LuaInvokeBinder binder;
            if (invokeBinders.TryGetValue(callInfo, out binder))
                return binder;

            return invokeBinders[callInfo] = new LuaInvokeBinder(callInfo);
        }

        public ConvertBinder GetConvertBinder(Type type)
        {
            LuaConvertBinder binder;
            if (convertBinders.TryGetValue(type, out binder))
                return binder;

            return convertBinders[type] = new LuaConvertBinder(type);
        }

        public SetMemberBinder GetSetMemberBinder(string name)
        {
            LuaSetMemberBinder binder;
            if (setMemberBinders.TryGetValue(name, out binder))
                return binder;

            return setMemberBinders[name] = new LuaSetMemberBinder(name);
        }

        public SetIndexBinder GetSetIndexBinder()
        {
            return setIndexBinder ?? (setIndexBinder = new LuaSetIndexBinder());
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
