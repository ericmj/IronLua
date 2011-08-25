using System;
using System.Dynamic;
using System.Linq.Expressions;
using Expr = System.Linq.Expressions.Expression;

namespace IronLua.Runtime.Binder
{
    class LuaBinaryOperationBinder : BinaryOperationBinder
    {
        Enviroment enviroment;

        public LuaBinaryOperationBinder(Enviroment enviroment, ExpressionType op)
            : base(op)
        {
            this.enviroment = enviroment;
        }

        public override DynamicMetaObject FallbackBinaryOperation(DynamicMetaObject target, DynamicMetaObject arg, DynamicMetaObject errorSuggestion)
        {
            // TODO: Check metatables if ConvertOperand() returns null (Dictionary<Type,LuaTable> in enviroment)

            if (!target.HasValue || !arg.HasValue)
                return Defer(target, arg);

            var restrictions =
                target.Restrictions
                    .Merge(arg.Restrictions)
                    .Merge(BindingRestrictions.GetTypeRestriction(target.Expression, target.LimitType))
                    .Merge(BindingRestrictions.GetTypeRestriction(arg.Expression, arg.LimitType));

            var left = ConvertOperand(target);
            var right = ConvertOperand(arg);
            var result = Expr.Convert(Expr.MakeBinary(Operation, left, right), typeof(object));

            return new DynamicMetaObject(result, restrictions);
        }

        Expr ConvertOperand(DynamicMetaObject metaObject)
        {
            if (metaObject.LimitType == typeof(double))
                return metaObject.Expression;
            if (metaObject.LimitType == typeof(string))
                return
                    Expr.Convert(
                        Expr.Dynamic(
                            enviroment.BinderCache.GetInvokeMemberBinder("tonumber", new CallInfo(1)),
                            typeof(object),
                            Expr.Constant(enviroment.Globals),
                            metaObject.Expression, Expr.Constant(10, typeof(int?))),
                        typeof(double));
            return null;
        }
    }
}