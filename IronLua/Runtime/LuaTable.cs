using System;
using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using IronLua.Runtime.Binder;
using Expr = System.Linq.Expressions.Expression;
using System.Text;
using IronLua.Util;

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
            int lastNum = 0;
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
                var expression =
                    Expr.Dynamic(new LuaGetMemberBinder(binder.Name), typeof(object), Expression);

                return binder.FallbackInvoke(new DynamicMetaObject(expression, Restrictions), args, null);
            }

            public override DynamicMetaObject BindSetIndex(SetIndexBinder binder, DynamicMetaObject[] indexes, DynamicMetaObject value)
            {
                var expression =
                    Expr.Call(
                        Expression,
                        typeof(LuaTable).GetMethod("SetValue", BindingFlags.NonPublic | BindingFlags.Instance),
                        indexes[0].Expression,
                        value.Expression);

                return new DynamicMetaObject(expression, this.MergeTypeRestrictions());
            }

            public override DynamicMetaObject BindSetMember(SetMemberBinder binder, DynamicMetaObject value)
            {
                var expression =
                    Expr.Call(
                        Expression,
                        typeof(LuaTable).GetMethod("SetValue", BindingFlags.NonPublic | BindingFlags.Instance),
                        Expr.Constant(binder.Name),
                        value.Expression);

                return new DynamicMetaObject(expression, this.MergeTypeRestrictions());
            }

            public override DynamicMetaObject BindGetMember(GetMemberBinder binder)
            {
                var restrictions = this.MergeTypeRestrictions();

                var expression =
                    Expr.Call(
                        Expression,
                        typeof(LuaTable).GetMethod("GetValue", BindingFlags.NonPublic | BindingFlags.Instance),
                        Expr.Constant(binder.Name));

                return new DynamicMetaObject(expression, restrictions);
            }

            public override DynamicMetaObject BindGetIndex(GetIndexBinder binder, DynamicMetaObject[] indexes)
            {
                throw new NotImplementedException();
            }
        }
    }
}
