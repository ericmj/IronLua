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
    class LuaFunction : IDynamicMetaObjectProvider
    {
        Delegate function;
        string[] parameters;
        bool hasVarargs;

        public LuaFunction(Delegate function, string[] parameters, bool hasVarargs)
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
                // TODO: Named arguments and varargs
                var restrictions = this
                    .MergeTypeRestrictions(args)
                    .Merge(BindingRestrictions.GetInstanceRestriction(Expression, Value));

                var function = (LuaFunction)Value;

                var resizeArgs = OverloadArgs(function, args);

                var expression =
                    Expr.Convert(
                        Expr.Invoke(
                            Expr.Convert(Expr.Constant(function.function), function.function.GetType()),
                            resizeArgs),
                        typeof(object));

                return new DynamicMetaObject(expression, restrictions);
            }

            IEnumerable<Expr> OverloadArgs(LuaFunction function, DynamicMetaObject[] args)
            {
                var numParams = function.parameters.Length;
                return args
                    .Select(arg => (Expr)Expr.Convert(arg.Expression, typeof(object)))
                    .Resize(numParams, Expr.Constant(null));
            }
        }
    }
}
