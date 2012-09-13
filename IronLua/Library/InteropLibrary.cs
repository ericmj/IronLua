using IronLua.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace IronLua.Library
{
    class InteropLibrary : Library
    {
        public InteropLibrary(LuaContext context, params Type[] types)
            : base(context)
        {

        }
        
        public override void Setup(Runtime.LuaTable table)
        {
            table.SetConstant("import", (Func<string, LuaTable>)ImportType);
        }

        private LuaTable ImportType(string typeName)
        {
            var type = Type.GetType(typeName, false);
            if(type != null)
            {
                var table = GenerateMetatable(type);
                Context.SetTypeMetatable(type, table);
                return table;
            }
            return null;
        }

        public void ImportType(Type type)
        {
            Context.SetTypeMetatable(type, GenerateMetatable(type));
        }

        internal LuaTable GenerateMetatable(Type type)
        {
            LuaTable table = new LuaTable();
            table.SetValue("__clrtype", type);
            table.SetValue(Constant.INDEX_METAMETHOD, (Func<object, object, object>)InteropIndex);
            table.SetValue(Constant.NEWINDEX_METAMETHOD, (Func<object, object, object, object>)InteropNewIndex);
            table.SetValue(Constant.CALL_METAMETHOD, (Func<object, Varargs, object>)InteropCall);

            return table;
        }

        private static object InteropIndex(object target, object index)
        {
            var type = (target as LuaTable).GetValue("__clrtype") as Type;
            var properties = type.GetProperties(
                BindingFlags.GetProperty |
                BindingFlags.GetField |
                BindingFlags.Public |
                BindingFlags.Static);

            //First check if there are any properties/fields with the specified name
            string indexKey = index.ToString();
            var property = properties.FirstOrDefault(x => x.Name.Equals(indexKey));
            if (property != default(PropertyInfo))
                return property.GetValue(target, null);
            
            //Check if we have any methods with the given name
            var methods = type.GetMethods(BindingFlags.InvokeMethod | BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance)
                .Where(x => x.Name.Equals(indexKey)).ToArray();
            if (methods.Length > 0)
                return new MethodIndex(indexKey, target is LuaTable ? null : target, type);

            //Then check if there are any indexers on the object and try them
            property = properties.FirstOrDefault(x => x.GetIndexParameters().Length == 1);
            if (property != default(PropertyInfo))
                return property.GetValue(target, new[] { index });

            throw new LuaRuntimeException("Undefined field or property '{0}' on {1}", indexKey, type.FullName);
        }

        private static object InteropNewIndex(object target, object index, object value)
        {
            var type = (target as LuaTable).GetValue("__clrtype") as Type;
            var properties = type.GetProperties(
                BindingFlags.SetProperty |
                BindingFlags.SetField |
                BindingFlags.Public |
                BindingFlags.Static);

            //First check if there are any properties/fields with the specified name
            string indexKey = index.ToString();
            var property = properties.FirstOrDefault(x => x.Name.Equals(indexKey));
            if (property != default(PropertyInfo))
            {
                property.SetValue(target, value, null);
                return value;
            }

            //Then check if there are any indexers on the object and try them
            property = properties.FirstOrDefault(x => x.GetIndexParameters().Length == 1);
            if (property != default(PropertyInfo))
            {
                property.SetValue(target, value, new[] { index });
                return value;
            }

            throw new LuaRuntimeException("Undefined field or property '{0}' on {1}", indexKey, type.FullName);
        }

        /// <summary>
        /// Acts upon a call to the specified interop type object. Behaves like a constructor call
        /// </summary>
        /// <param name="target">The interop type object being called</param>
        /// <param name="parameters">The parameters passed to the constructor</param>
        /// <returns>Returns the new instance of the interop type</returns>
        private static object InteropCall(object target, Varargs parameters)
        {
            var type = (target as LuaTable).GetValue("__clrtype") as Type;

            var methodName = parameters[0].ToString();
            var args = parameters.Skip(1).ToArray();
            var argsTypes = args.Select(x => x.GetType()).ToArray();

            
            var constructor = type.GetConstructor(argsTypes);

            if (constructor == null)
                throw new LuaRuntimeException("Cannot create instance of '" + type.FullName + "' with the given parameters");

            return constructor.Invoke(args);
        }

        private static bool ParamsMatch(MethodInfo method, Type[] paramTypes)
        {
            var parameters = method.GetParameters();
            bool hasParams = false;
            for (int i = 0; i < parameters.Length; i++)
            {
                //Test if there are too many required parameters
                if (i >= paramTypes.Length && !parameters[i].IsOptional)
                    return false;

                //Test if the parameter's types match or not
                if (paramTypes[i] != parameters[i].ParameterType &&
                    !parameters[i].ParameterType.IsAssignableFrom(paramTypes[i]) &&
                    !(hasParams = (i == parameters.Length - 1))) //This is in case we have a possible params argument
                    return false;
            }

            //If we have checked everything, then it's fine
            if (!hasParams)
                return true;

            //Check if we have any params args...
            var paramsType = parameters.Last().ParameterType;
            if(!paramsType.IsArray)           
                return false;

            var paramItemsType = paramsType.GetElementType();
            for (int i = parameters.Length - 1; i < paramTypes.Length; i++)

                //Make sure that all the params arguments are valid
                if (!paramItemsType.IsAssignableFrom(paramTypes[i]))
                    return false;

            return true;
        }

        public class MethodIndex
        {
            internal MethodIndex(string methodName, object target, Type clrType)
            {
                MethodName = methodName;
                Target = target;
                CLRType = clrType;
            }

            public string MethodName
            { get; private set; }

            public Type CLRType
            { get; private set; }

            public object Target
            { get; private set; }
        }
    }
}
