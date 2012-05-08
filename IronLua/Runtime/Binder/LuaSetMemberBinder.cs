using System;
using System.Diagnostics.Contracts;
using System.Dynamic;
using System.Linq.Expressions;

namespace IronLua.Runtime.Binder
{
    class LuaSetMemberBinder : SetMemberBinder
    {
        private readonly LuaContext _context;

        public LuaSetMemberBinder(LuaContext context, string name, bool ignoreCase = false)
            : base(name, ignoreCase)
        {
            Contract.Requires(context != null);
            _context = context;
        }

        public LuaContext Context
        {
            get { return _context; }
        }

        public override DynamicMetaObject FallbackSetMember(
            DynamicMetaObject target, 
            DynamicMetaObject value, 
            DynamicMetaObject errorSuggestion)
        {
            // Defer if any object has no value so that we evaulate their
            // Expressions and nest a CallSite for the InvokeMember.
            //if (!target.HasValue) 
            //    return Defer(target);
            //throw new NotImplementedException();

            return ErrorMetaObject(ReturnType, target, new DynamicMetaObject[] { value }, errorSuggestion);
        }

        static DynamicMetaObject ErrorMetaObject(Type resultType, DynamicMetaObject target, DynamicMetaObject[] args, DynamicMetaObject errorSuggestion)
        {
            return errorSuggestion ?? 
                new DynamicMetaObject(
                    Expression.Throw(Expression.New(typeof(NotImplementedException)), resultType),
                    target.Restrictions.Merge(BindingRestrictions.Combine(args))
                );
        }
    }
}