using System;
using System.Dynamic;
using System.Linq;
using Expr = System.Linq.Expressions.Expression;

namespace IronLua.Runtime.Binder
{
    class LuaInvokeBinder : InvokeBinder
    {
        public LuaInvokeBinder(CallInfo callInfo)
            : base(callInfo)
        {
        }

        public override DynamicMetaObject FallbackInvoke(DynamicMetaObject target, DynamicMetaObject[] args, DynamicMetaObject errorSuggestion)
        {
            throw new NotImplementedException();
        }
    }
}