using System;
using System.Dynamic;
using System.Linq;
using Microsoft.Scripting.Utils;
using Expr = System.Linq.Expressions.Expression;

namespace IronLua.Runtime.Binder
{
    class LuaInvokeMemberBinder : InvokeMemberBinder
    {
        readonly LuaContext context;

        public LuaInvokeMemberBinder(LuaContext context, string name, CallInfo callInfo)
            : base(name, false, callInfo)
        {
            ContractUtils.RequiresNotNull(context, "context");
            this.context = context;
        }

        public override DynamicMetaObject FallbackInvokeMember(DynamicMetaObject target, DynamicMetaObject[] args, DynamicMetaObject errorSuggestion)
        {
            throw new InvalidOperationException();
        }

        public override DynamicMetaObject FallbackInvoke(DynamicMetaObject target, DynamicMetaObject[] args, DynamicMetaObject errorSuggestion)
        {
            var combinedArgs = new Expr[args.Length + 1];
            combinedArgs[0] = target.Expression;
            Array.Copy(args.Select(a => a.Expression).ToArray(), 0, combinedArgs, 1, args.Length);

            var restrictions = target.Restrictions.Merge(BindingRestrictions.Combine(args));
            var expression =
                Expr.Dynamic(
                    context.CreateInvokeBinder(new CallInfo(args.Length)),
                    typeof(object),
                    combinedArgs);

            return new DynamicMetaObject(expression, restrictions);
        }
    }
}
