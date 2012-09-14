using Microsoft.Scripting;
using System;
using System.Diagnostics.Contracts;
using System.Dynamic;
using System.Linq.Expressions;

namespace IronLua.Runtime.Binder
{
    class LuaGetMemberBinder : GetMemberBinder
    {
        private readonly LuaContext _context;

        public LuaGetMemberBinder(LuaContext context, string name, bool ignoreCase = false)
            : base(name, ignoreCase)
        {
            Contract.Requires(context != null);
            _context = context;
        }

        public LuaContext Context
        {
            get { return _context; }
        }

        DynamicMetaObject MakeScriptScopeGetMember(DynamicMetaObject target, string name)
        {
            var getMemberExpression = Expression.PropertyOrField(Expression.Convert(target.Expression, target.LimitType), name);
            return new DynamicMetaObject(getMemberExpression, BindingRestrictions.Empty);
        }

        private DynamicMetaObject WrapToObject(DynamicMetaObject obj)
        {
            if (obj.LimitType != typeof(object))
                return new DynamicMetaObject(Expression.Convert(obj.Expression, typeof(object)), obj.Restrictions, obj.Value as object);
            return obj;
        }

        public override DynamicMetaObject FallbackGetMember(
            DynamicMetaObject target, 
            DynamicMetaObject errorSuggestion)
        {
            // Defer if any object has no value so that we evaulate their
            // Expressions and nest a CallSite for the InvokeMember.
            if (!target.HasValue) 
                return Defer(target);


            if (target.LimitType == typeof(IDynamicMetaObjectProvider))
                return WrapToObject(base.FallbackGetMember(target));

            else if (target.LimitType == typeof(LuaTable))
                return WrapToObject(base.FallbackGetMember(target));

            return WrapToObject(_context.Binder.GetMember(Name, target));            
        }
    }
}