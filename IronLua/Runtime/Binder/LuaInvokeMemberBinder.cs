using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using Expr = System.Linq.Expressions.Expression;

namespace IronLua.Runtime.Binder
{
    class LuaInvokeMemberBinder : InvokeMemberBinder
    {
        Enviroment enviroment;

        public LuaInvokeMemberBinder(Enviroment enviroment, string name, CallInfo callInfo)
            : base(name, false, callInfo)
        {
            this.enviroment = enviroment;
        }

        public override DynamicMetaObject FallbackInvokeMember(DynamicMetaObject target, DynamicMetaObject[] args, DynamicMetaObject errorSuggestion)
        {
            throw new NotImplementedException();
        }

        public override DynamicMetaObject FallbackInvoke(DynamicMetaObject target, DynamicMetaObject[] args, DynamicMetaObject errorSuggestion)
        {
            var combinedArgs = new Expr[args.Length + 1];
            combinedArgs[0] = target.Expression;
            Array.Copy(args.Select(a => a.Expression).ToArray(), 0, combinedArgs, 1, args.Length);

            var restrictions = target.Restrictions.Merge(BindingRestrictions.Combine(args));
            var expression =
                Expr.Dynamic(
                    enviroment.BinderCache.GetInvokeBinder(new CallInfo(args.Length)),
                    typeof(object),
                    combinedArgs);

            return new DynamicMetaObject(expression, restrictions);
        }
    }
}
