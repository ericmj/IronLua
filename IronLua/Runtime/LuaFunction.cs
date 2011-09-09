using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using IronLua.Runtime.Binder;
using IronLua.Util;
using Expr = System.Linq.Expressions.Expression;

namespace IronLua.Runtime
{
    public class LuaFunction : IDynamicMetaObjectProvider
    {
        Delegate function;
        List<string> parameters;
        bool hasVarargs;

        public LuaFunction(Delegate function, List<string> parameters, bool hasVarargs)
        {
            this.function = function;
            this.parameters = parameters;
            this.hasVarargs = hasVarargs;
        }

        public DynamicMetaObject GetMetaObject(Expression parameter)
        {
            return new MetaFunction(parameter, BindingRestrictions.Empty, this);
        }

        class MetaFunction : DynamicMetaObject
        {
            public MetaFunction(Expression expression, BindingRestrictions restrictions, object value)
                : base(expression, restrictions, value)
            {
            }

            public override DynamicMetaObject BindInvoke(InvokeBinder binder, DynamicMetaObject[] args)
            {
                // TODO: Named arguments
                var restrictions = this
                    .MergeTypeRestrictions(args)
                    .Merge(BindingRestrictions.GetInstanceRestriction(Expression, Value));

                var function = (LuaFunction)Value;

                var resizeArgs = OverloadArgs(function, args, restrictions);

                var expression =
                    Expr.Convert(
                        Expr.Invoke(
                            Expr.Convert(Expr.Constant(function.function), function.function.GetType()),
                            resizeArgs),
                        typeof(object));

                return new DynamicMetaObject(expression, restrictions);
            }

            IEnumerable<Expr> OverloadArgs(LuaFunction function, DynamicMetaObject[] args, BindingRestrictions restrictions)
            {
                if (args.Length == 0)
                    return new Expr[] {};

                var argExprs = ExpandVarargs(args, restrictions);

                var numParams = function.parameters.Count;
                if (function.hasVarargs)
                {
                    var listArgExprs = argExprs.ToList();
                    var constructor = typeof(Varargs).GetConstructor(new[] {typeof(object[])});
                    var varargsExpr = Expr.NewArrayInit(typeof(object), listArgExprs.Skip(function.parameters.Count));

                    return listArgExprs
                        .Take(function.parameters.Count)
                        .Add(Expr.New(constructor, varargsExpr));
                }

                return argExprs.Resize(numParams, Expr.Constant(null));
            }

            static IEnumerable<Expression> ExpandVarargs(DynamicMetaObject[] args, BindingRestrictions restrictions)
            {
                var argExprs = args
                    .Take(args.Length - 1)
                    .Select(TryWrapWithVarargsSelect);

                var lastArg = args.Last();
                if (lastArg.LimitType == typeof(Varargs))
                {
                    var varargs = (Varargs)lastArg.Value;
                    argExprs = argExprs.Concat(ExpandVarargsIntoExprs(varargs));
                    BindingRestrictions.GetInstanceRestriction(lastArg.Expression, lastArg.Value).Merge(restrictions);
                }
                else
                {
                    argExprs = argExprs.Add(Expr.Convert(lastArg.Expression, typeof(object)));
                }

                return argExprs;
            }

            static IEnumerable<Expr> ExpandVarargsIntoExprs(Varargs varargs)
            {
                var exprs = varargs.Take(varargs.Count - 1).Select(Expr.Constant);
                var lastArg = varargs.Last();

                if (lastArg.GetType() == typeof(Varargs))
                    return exprs.Concat(ExpandVarargsIntoExprs((Varargs)lastArg));
                return exprs.Add(Expr.Constant(lastArg));
            }

            static Expr TryWrapWithVarargsSelect(DynamicMetaObject dmo)
            {
                Expr expr;
                if (dmo.LimitType == typeof(Varargs))
                    expr = Expr.Call(dmo.Expression, typeof(Varargs).GetMethod("First"));
                else
                    expr = dmo.Expression;

                return Expr.Convert(expr, typeof(object));
            }
        }
    }
}
