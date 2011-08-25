using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Text;

namespace IronLua.Runtime.Binder
{
    class BinderCache
    {
        Enviroment enviroment;
        Dictionary<ExpressionType, LuaBinaryOperationBinder> binaryOperationBinders;
        Dictionary<InvokeMemberBinderKey, LuaInvokeMemberBinder> invokeMemberBinders;
        Dictionary<CallInfo, LuaInvokeBinder> invokeBinders;

        public BinderCache(Enviroment enviroment)
        {
            this.enviroment = enviroment;
            binaryOperationBinders = new Dictionary<ExpressionType, LuaBinaryOperationBinder>();
            invokeMemberBinders = new Dictionary<InvokeMemberBinderKey, LuaInvokeMemberBinder>();
            invokeBinders = new Dictionary<CallInfo, LuaInvokeBinder>();
        }

        public BinaryOperationBinder GetBinaryOperationBinder(ExpressionType op)
        {
            LuaBinaryOperationBinder binder;
            if (binaryOperationBinders.TryGetValue(op, out binder))
                return binder;

            binder = new LuaBinaryOperationBinder(enviroment, op);
            return binaryOperationBinders[op] = binder;
        }

        public InvokeMemberBinder GetInvokeMemberBinder(string name, CallInfo info)
        {
            var key = new InvokeMemberBinderKey(name, info);
            LuaInvokeMemberBinder binder;
            if (invokeMemberBinders.TryGetValue(key, out binder))
                return binder;

            binder = new LuaInvokeMemberBinder(enviroment, name, info);
            invokeMemberBinders[key] = binder;
            return binder;
        }

        public InvokeBinder GetInvokeBinder(CallInfo callInfo)
        {
            LuaInvokeBinder binder;
            if (invokeBinders.TryGetValue(callInfo, out binder))
                return binder;

            binder = new LuaInvokeBinder(callInfo);
            invokeBinders[callInfo] = binder;
            return binder;
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
