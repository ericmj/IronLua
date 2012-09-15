using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.Linq;
using IronLua.Runtime.Binder;
using IronLua.Util;
using Expr = System.Linq.Expressions.Expression;
using ExprType = System.Linq.Expressions.ExpressionType;
using IronLua.Hosting;

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

        LuaContext Context
        { get; set; }

        [Obsolete("This constructor is not safe when making use of metatables and metamethods")]
        public LuaTable()
            : this(null)
        {

        }

        public LuaTable(LuaContext context)
        {
            Context = context;

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
            return new MetaTable(Context, parameter, BindingRestrictions.Empty, this);
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
                        throw new LuaRuntimeException(Context, "Cannot change the value of the constant {0}", key);
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
                        throw new LuaRuntimeException(Context, "The constant {0} is already set to {1} and cannot be modified", key, value);
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

        internal bool HasValue(object key)
        {
            var pos = FindEntry(key);
            return pos >= 0;
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

        /// <summary>
        /// Gets the total number of sequentially indexed values in the table (ignoring non-integer keys)
        /// </summary>
        /// <returns>Returns the number of sequentially indexed values in the table</returns>
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

        /// <summary>
        /// Gets the total number of elements in the table
        /// </summary>
        internal int Count()
        {
            return entries.Count(x => x.Key != null);
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
            public MetaTable(LuaContext context, Expr expression, BindingRestrictions restrictions, LuaTable value)
                : base(expression, restrictions, value)
            {
                Context = context;
            }

            private LuaContext Context
            { get; set; }

            public override DynamicMetaObject BindBinaryOperation(BinaryOperationBinder binder, DynamicMetaObject arg)
            {
                if (!LuaBinaryOperationBinder.BinaryExprTypes.ContainsKey(binder.Operation))
                    throw new LuaRuntimeException(Context, "operation {0} not defined for table", binder.Operation.ToString());

                var expression = MetamethodFallbacks.BinaryOp(Context, binder.Operation, this, arg);
                return new DynamicMetaObject(expression, RuntimeHelpers.MergeTypeRestrictions(this));
            }

            public override DynamicMetaObject BindUnaryOperation(UnaryOperationBinder binder)
            {
                if (binder.Operation != ExprType.Negate)
                    throw new LuaRuntimeException(Context, "operation {0} not defined for table", binder.Operation.ToString());

                var expression = MetamethodFallbacks.UnaryMinus(Context, this);
                return new DynamicMetaObject(expression, RuntimeHelpers.MergeTypeRestrictions(this));
            }

            public override DynamicMetaObject BindInvoke(InvokeBinder binder, DynamicMetaObject[] args)
            {
                var expression = MetamethodFallbacks.Call(Context, this, args);
                return new DynamicMetaObject(expression, RuntimeHelpers.MergeTypeRestrictions(this));
            }

            public override DynamicMetaObject BindInvokeMember(InvokeMemberBinder binder, DynamicMetaObject[] args)
            {
                var expression = Expr.Dynamic(new LuaGetMemberBinder(Context, binder.Name), typeof(object), Expression);
                return binder.FallbackInvoke(new DynamicMetaObject(expression, Restrictions), args, null);
            }

            public override DynamicMetaObject BindGetMember(GetMemberBinder binder)
            {
                //var expression = Expr.Call(
                //    Expr.Convert(Expression, typeof(LuaTable)),
                //    MemberInfos.LuaTableGetValue,
                //    Expr.Constant(binder.Name));

                //return new DynamicMetaObject(expression, RuntimeHelpers.MergeTypeRestrictions(this));

                var valueVar = Expr.Variable(typeof(object), "$bindgetindex_valueVar$");

                var getValue = Expr.Call(
                    Expr.Convert(Expression, typeof(LuaTable)),
                    MemberInfos.LuaTableGetValue,
                    Expr.Convert(Expr.Constant(binder.Name), typeof(object)));
                var valueAssign = Expr.Assign(valueVar, getValue);

                var expression = Expr.Block(
                    new[] { valueVar },
                    valueAssign,
                    Expr.Condition(
                        Expr.Equal(valueVar, Expr.Constant(null)),
                        MetamethodFallbacks.Index(Context, this, new []{ new DynamicMetaObject(Expr.Constant(binder.Name), BindingRestrictions.Empty, binder.Name) }),
                        valueVar));

                return new DynamicMetaObject(expression, RuntimeHelpers.MergeTypeRestrictions(this));
            }

            public override DynamicMetaObject BindSetMember(SetMemberBinder binder, DynamicMetaObject value)
            {
                //var expression = Expr.Call(
                //    Expr.Convert(Expression, typeof(LuaTable)),
                //    MemberInfos.LuaTableSetValue,
                //    Expr.Constant(binder.Name),
                //    Expr.Convert(value.Expression, typeof(object)));

                //return new DynamicMetaObject(expression, RuntimeHelpers.MergeTypeRestrictions(this));

                var getValue = Expr.Call(
                    Expr.Convert(Expression, typeof(LuaTable)),
                    MemberInfos.LuaTableGetValue,
                    Expr.Convert(Expr.Constant(binder.Name), typeof(object)));

                var setValue = Expr.Call(
                    Expr.Convert(Expression, typeof(LuaTable)),
                    MemberInfos.LuaTableSetValue,
                    Expr.Convert(Expr.Constant(binder.Name), typeof(object)),
                    Expr.Convert(value.Expression, typeof(object)));

                var expression = Expr.Condition(
                    Expr.Equal(getValue, Expr.Constant(null)),
                    MetamethodFallbacks.NewIndex(Context, this, new[] { new DynamicMetaObject(Expr.Constant(binder.Name), BindingRestrictions.Empty, binder.Name) }, value),
                    setValue);

                return new DynamicMetaObject(expression, RuntimeHelpers.MergeTypeRestrictions(this));
            }

            public override DynamicMetaObject BindGetIndex(GetIndexBinder binder, DynamicMetaObject[] indexes)
            {
                var valueVar = Expr.Variable(typeof(object),"$bindgetindex_valueVar$");

                var getValue = Expr.Call(
                    Expr.Convert(Expression, typeof(LuaTable)),
                    MemberInfos.LuaTableGetValue,
                    Expr.Convert(indexes[0].Expression, typeof(object)));
                var valueAssign = Expr.Assign(valueVar, getValue);

                var expression = Expr.Block(
                    new[] { valueVar },
                    valueAssign,
                    Expr.Condition(
                        Expr.Equal(valueVar, Expr.Constant(null)),
                        MetamethodFallbacks.Index(Context, this, indexes),
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
                    MetamethodFallbacks.NewIndex(Context, this, indexes, value),
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

            [DebuggerBrowsable(DebuggerBrowsableState.Collapsed)]
            public LuaContext Context
            {
                get
                {
                    return table.Context;
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
