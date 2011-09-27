using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Linq.Expressions;
using IronLua.Runtime.Binder;
using IronLua.Util;
using Expr = System.Linq.Expressions.Expression;

namespace IronLua.Runtime
{
    class LuaTable : IDynamicMetaObjectProvider
    {
        int[] buckets;
        Entry[] entries;
        int freeList;
        int freeCount;
        int count;

        public LuaTable()
        {
            const int prime = 3;

            buckets = new int[prime];
            for (var i = 0; i < buckets.Length; i++)
                buckets[i] = -1;

            entries = new Entry[prime];
            freeList = -1;
        }

        public LuaTable Metatable { get; set; }

        public DynamicMetaObject GetMetaObject(Expression parameter)
        {
            return new MetaTable(parameter, BindingRestrictions.Empty, this);
        }

        internal object SetValue(object key, object value)
        {
            if (value == null)
            {
                Remove(key);
                return null;
            }

            var hashCode = key.GetHashCode() & Int32.MaxValue;
            var modHashCode = hashCode % buckets.Length;

            for (var i = buckets[modHashCode]; i >= 0; i = entries[i].Next)
            {
                if (entries[i].HashCode == hashCode && entries[i].Key.Equals(key))
                {
                    entries[i].Value = value;
                    return value;
                }
            }

            int free;
            if (freeCount > 0)
            {
                free = freeList;
                freeList = entries[free].Next;
                freeCount--;
            }
            else
            {
                if (count == entries.Length)
                {
                    Resize();
                    modHashCode = hashCode % buckets.Length;
                }
                free = count;
                count++;
            }

            entries[free].HashCode = hashCode;
            entries[free].Next = buckets[modHashCode];
            entries[free].Key = key;
            entries[free].Value = value;
            buckets[modHashCode] = free;
            return value;
        }

        public object GetValue(object key)
        {
            var pos = FindEntry(key);
            return pos < 0 ? null : entries[pos].Value;
        }

        void Remove(object key)
        {
            var hashCode = key.GetHashCode() & Int32.MaxValue;
            var modHashCode = hashCode % buckets.Length;
            var last = -1;

            for (var i = buckets[modHashCode]; i >= 0; i = entries[i].Next)
            {
                if (entries[i].HashCode == hashCode && entries[i].Key.Equals(key))
                {
                    if (last < 0)
                        buckets[modHashCode] = entries[i].Next;
                    else
                        entries[last].Next = entries[i].Next;

                    entries[i].HashCode = -1;
                    entries[i].Next = freeList;
                    entries[i].Key = null;
                    entries[i].Value = null;
                    freeList = i;
                    freeCount++;
                    return;
                }
                last = i;
            }
        }

        internal int FindEntry(object key)
        {
            var hashCode = key.GetHashCode() & Int32.MaxValue;
            var modHashCode = hashCode % buckets.Length;

            for (var i = buckets[modHashCode]; i >= 0; i = entries[i].Next)
            {
                if (entries[i].HashCode == hashCode && entries[i].Key.Equals(key))
                    return i;
            }

            return -1;
        }

        void Resize()
        {
            var prime = HashHelpers.GetPrime(count * 2);

            var newBuckets = new int[prime];
            for (var i = 0; i < newBuckets.Length; i++)
                newBuckets[i] = -1;

            var newEntries = new Entry[prime];
            Array.Copy(entries, 0, newEntries, 0, count);
            for (var i = 0; i < count; i++)
            {
                var modHashCode = newEntries[i].HashCode % prime;
                newEntries[i].Next = newBuckets[modHashCode];
                newBuckets[modHashCode] = i;
            }

            buckets = newBuckets;
            entries = newEntries;
        }

        internal int Length()
        {
            var lastNum = 0;
            foreach (var key in entries.Select(e => e.Key).OfType<double>().OrderBy(key => key))
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

        class Entry
        {
            public int HashCode;
            public object Key;
            public object Value;
            public int Next;
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
