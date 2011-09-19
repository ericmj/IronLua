using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Linq.Expressions;
using IronLua.Runtime.Binder;
using Expr = System.Linq.Expressions.Expression;

namespace IronLua.Runtime
{
    class LuaTable : IDynamicMetaObjectProvider
    {
        readonly Dictionary<object, object> values;

        public LuaTable()
        {
            values = new Dictionary<object, object>();
        }

        public LuaTable Metatable { get; set; }

        public DynamicMetaObject GetMetaObject(Expression parameter)
        {
            return new MetaTable(parameter, BindingRestrictions.Empty, this);
        }

        internal object GetValue(object key)
        {
            object value;
            values.TryGetValue(key, out value);
            return value;
        }

        internal object SetValue(object key, object value)
        {
            if (value == null)
                values.Remove(key);
            else
                values[key] = value;
            return value;
        }

        internal int Length()
        {
            var lastNum = 0;
            foreach (var key in values.Keys.OfType<double>().OrderBy(key => key))
            {
                var intKey = (int)key;

                if (intKey > lastNum + 1)
                    return lastNum;
                if (intKey != key)
                    continue;
                
                lastNum = intKey;
            }
            return lastNum;
        }

        class MetaTable : DynamicMetaObject
        {
            public MetaTable(Expression expression, BindingRestrictions restrictions, LuaTable value)
                : base(expression, restrictions, value)
            {
            }

            public override DynamicMetaObject BindInvokeMember(InvokeMemberBinder binder, DynamicMetaObject[] args)
            {
                var expression = Expr.Dynamic(new LuaGetMemberBinder(binder.Name), typeof(object), Expression);
                return binder.FallbackInvoke(new DynamicMetaObject(expression, Restrictions), args, null);
            }

            public override DynamicMetaObject BindSetIndex(SetIndexBinder binder, DynamicMetaObject[] indexes, DynamicMetaObject value)
            {
                var expression = Expr.Call(
                    Expr.Convert(Expression, typeof(LuaTable)),
                    Methods.LuaTableSetValue,
                    Expr.Convert(indexes[0].Expression, typeof(object)),
                    Expr.Convert(value.Expression, typeof(object)));

                return new DynamicMetaObject(expression, RuntimeHelpers.MergeTypeRestrictions(this));
            }

            public override DynamicMetaObject BindSetMember(SetMemberBinder binder, DynamicMetaObject value)
            {
                var expression = Expr.Call(
                    Expr.Convert(Expression, typeof(LuaTable)),
                    Methods.LuaTableSetValue, 
                    Expr.Constant(binder.Name),
                    Expr.Convert(value.Expression, typeof(object)));

                return new DynamicMetaObject(expression, RuntimeHelpers.MergeTypeRestrictions(this));
            }

            public override DynamicMetaObject BindGetMember(GetMemberBinder binder)
            {
                var expression = Expr.Call(
                    Expr.Convert(Expression, typeof(LuaTable)),
                    Methods.LuaTableGetValue,
                    Expr.Constant(binder.Name));

                return new DynamicMetaObject(expression, RuntimeHelpers.MergeTypeRestrictions(this));
            }

            public override DynamicMetaObject BindGetIndex(GetIndexBinder binder, DynamicMetaObject[] indexes)
            {
                var expression = Expr.Call(
                    Expr.Convert(Expression, typeof(LuaTable)),
                    Methods.LuaTableGetValue,
                    Expr.Convert(indexes[0].Expression, typeof(object)));

                return new DynamicMetaObject(expression, RuntimeHelpers.MergeTypeRestrictions(this));
            }
        }
    }
}
