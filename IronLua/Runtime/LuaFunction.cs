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

                var argExprs = LuaInvokeBinder.ExpandVarargs(args, restrictions);

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
        }
    }
}
