using System;
using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Linq.Expressions;
using Expr = System.Linq.Expressions.Expression;
using System.Reflection;
using System.Text;

namespace IronLua.Runtime
{
    class LuaFunction : IDynamicMetaObjectProvider
    {
        Delegate function;
        IList<LuaParameter> parameters;

        public LuaFunction(Delegate function, IList<LuaParameter> parameters)
        {
            this.function = function;
            this.parameters = parameters;
        }

        public LuaFunction(Delegate function)
        {
            this.function = function;

            var parameterInfos = function.Method.GetParameters();
            parameters = new LuaParameter[parameterInfos.Length];

            for (int i = 0; i < parameterInfos.Length; i++)
            {
                var paramInfo = parameterInfos[i];
                var defaultValue = paramInfo.IsOptional ? paramInfo.DefaultValue : null;
                parameters[i] = new LuaParameter(paramInfo.Name, defaultValue);
            }
        }

        // NOTE: Invoking on the dynamic type is faster!
        internal object DynamicInvoke(params object[] args)
        {
            return function.DynamicInvoke(args);
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
                // TODO: Optional parameters and passing table for named parameters

                var restrictions = Restrictions.Merge(
                    BindingRestrictions.GetInstanceRestriction(Expression, Value));

                var luaFunction = (LuaFunction) Value;

                var expression =
                    Expr.Convert(
                        Expr.Invoke(
                            Expr.Constant(luaFunction.function, luaFunction.function.GetType()),
                            args.Select(a => a.Expression)),
                        typeof(object));

                return new DynamicMetaObject(expression, restrictions);
            }
        }
    }

    class LuaParameter
    {
        public string Name { get; private set; }
        public object DefaultValue { get; private set; }
        public bool IsOptional { get { return DefaultValue != null; } }

        public LuaParameter(string name, object defaultValue = null)
        {
            Name = name;
            DefaultValue = defaultValue;
        }
    }
}
