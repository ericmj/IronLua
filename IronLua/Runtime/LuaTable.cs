using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace IronLua.Runtime
{
    class LuaTable : IDynamicMetaObjectProvider
    {
        Dictionary<string, object> values;
        LuaTable metatable;

        public DynamicMetaObject GetMetaObject(Expression parameter)
        {
            return new MetaTable(parameter, BindingRestrictions.Empty, this);
        }

        class MetaTable : DynamicMetaObject
        {
            public MetaTable(Expression expression, BindingRestrictions restrictions) : base(expression, restrictions)
            {
            }

            public MetaTable(Expression expression, BindingRestrictions restrictions, object value) : base(expression, restrictions, value)
            {
            }
        }
    }
}
