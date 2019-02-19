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


        DynamicMetaObject MakeScriptScopeSetMember(DynamicMetaObject target, string name, Expression value)
        {
            var setMemberExpression = Expression.Block(typeof(object),
                Expression.Assign(Expression.PropertyOrField(Expression.Convert(target.Expression, target.LimitType), name), value),
                Expression.Convert(value, typeof(object)));
            return new DynamicMetaObject(setMemberExpression, BindingRestrictions.Empty);
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

            if (!target.HasValue || !value.HasValue)
                return Defer(target, value);

            if (target.LimitType == typeof(IDynamicMetaObjectProvider))
                return base.FallbackSetMember(target, value);

            return _context.Binder.SetMember(Name, target, value);

            //return MakeScriptScopeSetMember(target, Name, Expression.Convert(value.Expression, value.LimitType));

            //return ErrorMetaObject(ReturnType, target, new DynamicMetaObject[] { value }, errorSuggestion);
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