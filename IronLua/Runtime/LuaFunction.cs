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
        object function;
        Type functionType;
        IList<LuaParameter> parameters;

        public LuaFunction(object function, IList<LuaParameter> parameters)
        {
            this.function = function;
            functionType = function.GetType();
            this.parameters = parameters;
        }

        public static LuaFunction Create(object function, MethodInfo methodInfo)
        {
            var parameterInfos = methodInfo.GetParameters();
            var luaParameters = new LuaParameter[parameterInfos.Length];

            for (int i = 0; i < parameterInfos.Length; i++)
                luaParameters[i] = new LuaParameter(parameterInfos[i].Name, parameterInfos[i].DefaultValue);

            return new LuaFunction(function, luaParameters);
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
                // TODO: Passing tables for named parameters and optional parameters

                var restrictions = Restrictions.Merge(
                    BindingRestrictions.GetInstanceRestriction(Expression, Value));

                var luaFunction = (LuaFunction) Value;

                var expression =
                    Expr.Convert(
                        Expr.Invoke(
                            Expr.Constant(luaFunction.function, luaFunction.functionType),
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
