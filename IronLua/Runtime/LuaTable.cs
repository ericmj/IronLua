using System;
using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Linq.Expressions;
using Expr = System.Linq.Expressions.Expression;
using System.Text;

namespace IronLua.Runtime
{
    class LuaTable : IDynamicMetaObjectProvider
    {
        Dictionary<object, object> values;
        LuaTable metatable;

        public LuaTable()
        {
            values = new Dictionary<object, object>();
        }

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

        internal void SetValue(object key, object value)
        {
            values[key] = value;
        }

        internal int Length()
        {
            int lastNum = 0;
            foreach (var key in values.Keys.OfType<double>().OrderBy(key => key))
            {
                var intKey = (int)key;
                if (intKey != key)
                    continue;

                if (intKey > lastNum + 1)
                    return lastNum;
                
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
                // TODO: Move code Expr.Call to BindGetMember
                var restrictions = Restrictions.Merge(
                    BindingRestrictions.GetTypeRestriction(Expression, typeof(LuaTable)));

                var expression =
                    Expr.Call(
                        Expression,
                        typeof(LuaTable).GetMethod("GetValue"),
                        Expr.Constant(binder.Name));

                return binder.FallbackInvoke(new DynamicMetaObject(expression, restrictions), args, null);
            }
        }
    }
}
