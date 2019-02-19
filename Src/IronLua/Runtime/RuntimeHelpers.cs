using System;
using System.Dynamic;
using System.Linq;
using System.Linq.Expressions;

namespace IronLua.Runtime
{
    static class RuntimeHelpers
    {
        public static BindingRestrictions MergeTypeRestrictions(DynamicMetaObject dmo1, DynamicMetaObject[] dmos)
        {
            var newDmos = new DynamicMetaObject[dmos.Length + 1];
            newDmos[0] = dmo1;
            Array.Copy(dmos, 0, newDmos, 1, dmos.Length);
            return MergeTypeRestrictions(newDmos);
        }

        public static BindingRestrictions MergeTypeRestrictions(params DynamicMetaObject[] dmos)
        {
            var restrictions = BindingRestrictions.Combine(dmos);

            foreach (var dmo in dmos)
            {
                if (dmo.HasValue && dmo.Value == null)
                    restrictions = restrictions.Merge(BindingRestrictions.GetInstanceRestriction(dmo.Expression, dmo.Value));
                else
                    restrictions = restrictions.Merge(BindingRestrictions.GetTypeRestriction(dmo.Expression, dmo.LimitType));
            }

            return restrictions;
        }

        public static BindingRestrictions MergeInstanceRestrictions(params DynamicMetaObject[] dmos)
        {
            var restrictions = BindingRestrictions.Combine(dmos);
            return dmos.Aggregate(
                restrictions,
                (current, dmo) => current.Merge(BindingRestrictions.GetInstanceRestriction(dmo.Expression, dmo.Value)));
        }

        public static bool TryGetFirstVarargs(DynamicMetaObject target, out DynamicMetaObject first)
        {
            if (target.LimitType != typeof(Varargs))
            {
                first = target;
                return false;
            }

            Expression expr = Expression.Call(Expression.Convert(target.Expression, typeof(Varargs)),
                                              typeof(Varargs).GetMethod("First"));
            first = new DynamicMetaObject(expr, MergeTypeRestrictions(target));
            return true;
        }
    }
}
