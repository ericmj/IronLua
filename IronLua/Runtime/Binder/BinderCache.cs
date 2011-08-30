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

        public BinderCache(Context context)
        {
            this.context = context;
            binaryOperationBinders = new Dictionary<ExprType, LuaBinaryOperationBinder>();
            unaryOperationBinders = new Dictionary<ExprType, LuaUnaryOperationBinder>();
            invokeMemberBinders = new Dictionary<InvokeMemberBinderKey, LuaInvokeMemberBinder>();
            invokeBinders = new Dictionary<CallInfo, LuaInvokeBinder>();
        }

        public BinaryOperationBinder GetBinaryOperationBinder(ExprType operation)
        {
            LuaBinaryOperationBinder binder;
            if (binaryOperationBinders.TryGetValue(operation, out binder))
                return binder;

            binder = new LuaBinaryOperationBinder(context, operation);
            return binaryOperationBinders[operation] = binder;
        }

        public UnaryOperationBinder GetUnaryOperationBinder(ExprType operation)
        {
            LuaUnaryOperationBinder binder;
            if (unaryOperationBinders.TryGetValue(operation, out binder))
                return binder;

            binder = new LuaUnaryOperationBinder(context, operation);
            return unaryOperationBinders[operation] = binder;
        }

        public InvokeMemberBinder GetInvokeMemberBinder(string name, CallInfo info)
        {
            var key = new InvokeMemberBinderKey(name, info);
            LuaInvokeMemberBinder binder;
            if (invokeMemberBinders.TryGetValue(key, out binder))
                return binder;

            binder = new LuaInvokeMemberBinder(context, name, info);
            return invokeMemberBinders[key] = binder;
        }

        public InvokeBinder GetInvokeBinder(CallInfo callInfo)
        {
            LuaInvokeBinder binder;
            if (invokeBinders.TryGetValue(callInfo, out binder))
                return binder;

            binder = new LuaInvokeBinder(callInfo);
            return invokeBinders[callInfo] = binder;
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
