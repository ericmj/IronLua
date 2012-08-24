using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.Linq;
using IronLua.Runtime.Binder;
using IronLua.Util;
using Expr = System.Linq.Expressions.Expression;
using ExprType = System.Linq.Expressions.ExpressionType;

namespace IronLua.Runtime
{
#if DEBUG
    [DebuggerTypeProxy(typeof(LuaTableDebugView))]
#endif
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

        public DynamicMetaObject GetMetaObject(Expr parameter)
        {
            return new MetaTable(parameter, BindingRestrictions.Empty, this);
        }

        internal Varargs Next(object index = null)
        {
            if (index == null)
            {
                for (var i = 0; i < entries.Length; i++)
                {
                    if (entries[i].Key != null)
                        return new Varargs(entries[i].Key, entries[i].Value);
                }
            }
            else
            {
                for (var i = FindEntry(index) + 1; i < entries.Length; i++)
                {
                    if (entries[i].Key != null)
                        return new Varargs(entries[i].Key, entries[i].Value);
                }
            }
            return null;
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
                    if (entries[i].Locked)
                        throw new LuaRuntimeException("Cannot change the value of the constant {0}", key);
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

        internal object SetConstant(object key, object value)
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
                    if(entries[i].Locked)
                        throw new LuaRuntimeException("The constant {0} is already set to {1} and cannot be modified", key, value);
                    else
                    {
                        //TODO: Decide whether or not we should allow a variable to be converted into a constant
                        entries[i].Value = value;
                        entries[i].Locked = true;
                        return value;
                    }
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
            entries[free].Locked = true;
            buckets[modHashCode] = free;
            return value;
        }

        internal object GetValue(object key)
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

        int FindEntry(object key)
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

        struct Entry
        {
            public int HashCode;
            public object Key;
            public object Value;
            public int Next;
            public bool Locked;
        }

        class MetaTable : DynamicMetaObject
        {
            public MetaTable(Expr expression, BindingRestrictions restrictions, LuaTable value)
                : base(expression, restrictions, value)
            {
            }

            public override DynamicMetaObject BindBinaryOperation(BinaryOperationBinder binder, DynamicMetaObject arg)
            {
                if (!LuaBinaryOperationBinder.BinaryExprTypes.ContainsKey(binder.Operation))
                    throw new Exception(); // TODO

                var expression = MetamethodFallbacks.BinaryOp(null, binder.Operation, this, arg);
                return new DynamicMetaObject(expression, RuntimeHelpers.MergeTypeRestrictions(this));
            }

            public override DynamicMetaObject BindUnaryOperation(UnaryOperationBinder binder)
            {
                if (binder.Operation != ExprType.Negate)
                    throw new Exception(); // TODO

                var expression = MetamethodFallbacks.UnaryMinus(null, this);
                return new DynamicMetaObject(expression, RuntimeHelpers.MergeTypeRestrictions(this));
            }

            public override DynamicMetaObject BindInvoke(InvokeBinder binder, DynamicMetaObject[] args)
            {
                var expression = MetamethodFallbacks.Call(null, this, args);
                return new DynamicMetaObject(expression, RuntimeHelpers.MergeTypeRestrictions(this));
            }

            public override DynamicMetaObject BindInvokeMember(InvokeMemberBinder binder, DynamicMetaObject[] args)
            {
                var expression = Expr.Dynamic(new LuaGetMemberBinder(null, binder.Name), typeof(object), Expression);
                return binder.FallbackInvoke(new DynamicMetaObject(expression, Restrictions), args, null);
            }

            public override DynamicMetaObject BindGetMember(GetMemberBinder binder)
            {
                var expression = Expr.Call(
                    Expr.Convert(Expression, typeof(LuaTable)),
                    MemberInfos.LuaTableGetValue,
                    Expr.Constant(binder.Name));

                return new DynamicMetaObject(expression, RuntimeHelpers.MergeTypeRestrictions(this));
            }

            public override DynamicMetaObject BindSetMember(SetMemberBinder binder, DynamicMetaObject value)
            {
                var expression = Expr.Call(
                    Expr.Convert(Expression, typeof(LuaTable)),
                    MemberInfos.LuaTableSetValue,
                    Expr.Constant(binder.Name),
                    Expr.Convert(value.Expression, typeof(object)));

                return new DynamicMetaObject(expression, RuntimeHelpers.MergeTypeRestrictions(this));
            }

            public override DynamicMetaObject BindGetIndex(GetIndexBinder binder, DynamicMetaObject[] indexes)
            {
                var valueVar = Expr.Variable(typeof(object));

                var getValue = Expr.Call(
                    Expr.Convert(Expression, typeof(LuaTable)),
                    MemberInfos.LuaTableGetValue,
                    Expr.Convert(indexes[0].Expression, typeof(object)));
                var valueAssign = Expr.Assign(valueVar, getValue);

                var expression = Expr.Block(
                    valueVar,
                    Expr.Condition(
                        Expr.Equal(valueVar, Expr.Constant(null)),
                        MetamethodFallbacks.Index(null, this, indexes),
                        valueVar));

                return new DynamicMetaObject(expression, RuntimeHelpers.MergeTypeRestrictions(this));
            }

            public override DynamicMetaObject BindSetIndex(SetIndexBinder binder, DynamicMetaObject[] indexes, DynamicMetaObject value)
            {
                var getValue = Expr.Call(
                    Expr.Convert(Expression, typeof(LuaTable)),
                    MemberInfos.LuaTableGetValue,
                    Expr.Convert(indexes[0].Expression, typeof(object)));

                var setValue = Expr.Call(
                    Expr.Convert(Expression, typeof(LuaTable)),
                    MemberInfos.LuaTableSetValue,
                    Expr.Convert(indexes[0].Expression, typeof(object)),
                    Expr.Convert(value.Expression, typeof(object)));

                var expression = Expr.Condition(
                    Expr.Equal(getValue, Expr.Constant(null)),
                    MetamethodFallbacks.NewIndex(null, this, indexes, value),
                    setValue);

                return new DynamicMetaObject(expression, RuntimeHelpers.MergeTypeRestrictions(this));
            }
        }

#if DEBUG
        class LuaTableDebugView
        {
            LuaTable table;

            [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
            public KeyValuePair<object, object>[] Items
            {
                get
                {
                    var index = 0;
                    var pairs = new KeyValuePair<object, object>[table.count];

                    for (var i = 0; i < table.count; i++)
                    {
                        if (table.entries[i].HashCode >= 0)
                            pairs[index++] = new KeyValuePair<object, object>(table.entries[i].Key,
                                                                              table.entries[i].Value);
                    }

                    return pairs;
                }
            }

            public LuaTableDebugView(LuaTable table)
            {
                this.table = table;
            }
        }
#endif
    }
}
