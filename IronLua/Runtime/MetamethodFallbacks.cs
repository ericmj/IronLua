using System;
using System.Dynamic;
using System.Linq;
using Expr = System.Linq.Expressions.Expression;
using ExprType = System.Linq.Expressions.ExpressionType;

namespace IronLua.Runtime
{
    static class MetamethodFallbacks
    {
        public static Expr BinaryOp(LuaContext context, ExprType operation, DynamicMetaObject left, DynamicMetaObject right)
        {
            return Expr.Invoke(
                Expr.Constant((Func<LuaContext, ExprType, object, object, object>)LuaOps.BinaryOpMetamethod),
                Expr.Constant(context, typeof(LuaContext)),
                Expr.Constant(operation),
                Expr.Convert(left.Expression, typeof(object)),
                Expr.Convert(right.Expression, typeof(object)));
        }

        public static Expr Index(LuaContext context, DynamicMetaObject target, DynamicMetaObject[] indexes)
        {
            return Expr.Invoke(
                Expr.Constant((Func<LuaContext, object, object, object>)LuaOps.IndexMetamethod),
                Expr.Constant(context, typeof(LuaContext)),
                Expr.Convert(target.Expression, typeof(object)),
                Expr.Convert(indexes[0].Expression, typeof(object)));
        }

        public static Expr Call(LuaContext context, DynamicMetaObject target, DynamicMetaObject[] args)
        {
            var expression = Expr.Invoke(
                Expr.Constant((Func<LuaContext, object, object[], object>)LuaOps.CallMetamethod),
                Expr.Constant(context, typeof(LuaContext)),
                Expr.Convert(target.Expression, typeof(object)),
                Expr.NewArrayInit(
                    typeof(object),
                    args.Select(arg => Expr.Convert(arg.Expression, typeof(object)))));

            return expression;
        }

        public static Expr NewIndex(LuaContext context, DynamicMetaObject target, DynamicMetaObject[] indexes, DynamicMetaObject value)
        {
            return Expr.Invoke(
                Expr.Constant((Func<LuaContext, object, object, object, object>)LuaOps.NewIndexMetamethod),
                Expr.Constant(context, typeof(LuaContext)),
                Expr.Convert(target.Expression, typeof(object)),
                Expr.Convert(indexes[0].Expression, typeof(object)),
                Expr.Convert(value.Expression, typeof(object)));
        }

        public static Expr UnaryMinus(LuaContext context, DynamicMetaObject target)
        {
            return Expr.Invoke(
                Expr.Constant((Func<LuaContext, object, object>)LuaOps.UnaryMinusMetamethod),
                Expr.Constant(context, typeof(LuaContext)),
                Expr.Convert(target.Expression, typeof(object)));
        }
    }
}
