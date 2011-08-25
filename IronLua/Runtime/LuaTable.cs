using System;
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
        Dictionary<string, object> values;
        LuaTable metatable;

        public LuaTable()
        {
            values = new Dictionary<string, object>();
        }

        public DynamicMetaObject GetMetaObject(Expression parameter)
        {
            return new MetaTable(parameter, BindingRestrictions.Empty, this);
        }

        public object GetValue(string key)
        {
            object value;
            values.TryGetValue(key, out value);
            return value;
        }

        public void SetValue(string key, object value)
        {
            values[key] = value;
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
