using System.Diagnostics.Contracts;
using System.Dynamic;
using System.Reflection;
using System.Linq;
using System;
using Microsoft.Scripting.Actions;
using System.Linq.Expressions;

namespace IronLua.Runtime.Binder
{
    class LuaGetIndexBinder : GetIndexBinder
    {
        private readonly LuaContext _context;

        public LuaGetIndexBinder(LuaContext context, CallInfo callInfo)
            : base(callInfo)
        {
            Contract.Requires(context != null);
            _context = context;
        }

        public LuaGetIndexBinder(LuaContext context)
            : this(context, new CallInfo(1))
        {            
        }

        public LuaContext Context
        {
            get { return _context; }
        }
        
        private DynamicMetaObject WrapToObject(DynamicMetaObject obj)
        {
            if (obj.LimitType != typeof(object))
                return new DynamicMetaObject(Expression.Convert(obj.Expression, typeof(object)), obj.Restrictions, obj.Value as object);
            return obj;
        }

        public override DynamicMetaObject FallbackGetIndex(DynamicMetaObject target, DynamicMetaObject[] indexes, DynamicMetaObject errorSuggestion)
        {
            if (target.LimitType.GetMethods().Any(x => x.Name == "get_Item" && x.GetParameters().Length == CallInfo.ArgumentCount))
            {
                DynamicMetaObject[] args = new DynamicMetaObject[indexes.Length + 1];
                args[0] = target;
                Array.Copy(indexes, 0, args, 1, indexes.Length);

                var method = target.LimitType.GetMethods().Where(x => x.Name.Equals("get_Item") && x.GetParameters().Length == CallInfo.ArgumentCount).First();

                return WrapToObject(Context.Binder.MakeCallExpression(DefaultOverloadResolver.Factory, method, args));
            }

            var expression = MetamethodFallbacks.Index(_context, target, indexes);

            return WrapToObject(new DynamicMetaObject(expression, BindingRestrictions.Empty));
            
        }
    }
}