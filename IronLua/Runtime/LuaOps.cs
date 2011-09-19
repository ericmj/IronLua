using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using IronLua.Library;
using Expr = System.Linq.Expressions.Expression;
using ExprType = System.Linq.Expressions.ExpressionType;

namespace IronLua.Runtime
{
    static class LuaOps
    {
        public static bool Not(object value)
        {
            return value != null && (!(value is bool) || (bool)value);
        }

        public static object Length(Context context, object obj)
        {
            string str;
            LuaTable table;

            if ((str = obj as string) != null)
                return str.Length;
            if ((table = obj as LuaTable) != null)
                return table.Length();

            return LengthMetamethod(context, obj);
        }

        public static object Concat(Context context, object left, object right)
        {
            if ((left is string || left is double) && (right is double || right is string))
                return String.Concat(left, right);

            return ConcatMetamethod(context, left, right);
        }

        public static object LengthMetamethod(Context context, object obj)
        {
            dynamic metamethod = context.GetMetamethod(obj, Constant.LENGTH_METAMETHOD);
            if (metamethod != null)
                return metamethod(obj);

            string typeName;
            if (!RuntimeHelpers.TryGetTypeName(obj, out typeName))
                throw new ArgumentOutOfRangeException("obj", "Argument is of non-suported type");
            throw new LuaRuntimeException(ExceptionMessage.OP_TYPE_ERROR, "get length of", typeName);
        }

        public static object UnaryMinusMetamethod(Context context, object obj)
        {
            dynamic metamethod = context.GetMetamethod(obj, Constant.UNARYMINUS_METAMETHOD);
            if (metamethod != null)
                return metamethod(obj);

            string typeName;
            if (!RuntimeHelpers.TryGetTypeName(obj, out typeName))
                throw new ArgumentOutOfRangeException("obj", "Argument is of non-suported type");
            throw new LuaRuntimeException(ExceptionMessage.OP_TYPE_ERROR, "perform arithmetic on", typeName);
        }

        public static object IndexMetamethod(Context context, object obj, object key)
        {
            dynamic metamethod = context.GetMetamethod(obj, Constant.INDEX_METAMETHOD);

            if (metamethod != null)
            {
                if (metamethod is Delegate)
                    return metamethod(obj, key);
                if (metamethod is LuaTable)
                    return context.GetDynamicIndex()(obj, key);
            }

            string typeName;
            if (!RuntimeHelpers.TryGetTypeName(obj, out typeName))
                throw new ArgumentOutOfRangeException("obj", "Argument is of non-suported type");
            throw new LuaRuntimeException(ExceptionMessage.OP_TYPE_ERROR, "index", typeName);
        }

        public static object NewIndexMetamethod(Context context, object obj, object key, object value)
        {
            dynamic metamethod = context.GetMetamethod(obj, Constant.NEWINDEX_METAMETHOD);

            if (metamethod != null)
            {
                if (metamethod is Delegate)
                    return metamethod(obj, key, value);
                if (metamethod is LuaTable)
                    return context.GetDynamicNewIndex()(obj, key, value);
            }

            string typeName;
            if (!RuntimeHelpers.TryGetTypeName(obj, out typeName))
                throw new ArgumentOutOfRangeException("obj", "Argument is of non-suported type");
            throw new LuaRuntimeException(ExceptionMessage.OP_TYPE_ERROR, "index", typeName);
        }

        public static object CallMetamethod(Context context, object obj, object[] args)
        {
            dynamic metamethod = context.GetMetamethod(obj, Constant.CALL_METAMETHOD);
            if (metamethod != null)
                return context.GetDynamicCall2()(metamethod, obj, new Varargs(args));

            string typeName;
            if (!RuntimeHelpers.TryGetTypeName(obj, out typeName))
                throw new ArgumentOutOfRangeException("obj", "Argument is of non-suported type");
            throw new LuaRuntimeException(ExceptionMessage.OP_TYPE_ERROR, "call", typeName);
        }

        public static object ConcatMetamethod(Context context, object left, object right)
        {
            dynamic metamethod = context.GetMetamethod(left, Constant.CONCAT_METAMETHOD) ??
                                 context.GetMetamethod(right, Constant.CONCAT_METAMETHOD);
            if (metamethod != null)
                return metamethod(left, right);

            string typeName;
            if (left is string)
            {
                if (!RuntimeHelpers.TryGetTypeName(right, out typeName))
                    throw new ArgumentOutOfRangeException("right", "Argument is of non-suported type");
                Debug.Assert(typeName != "string");
            }
            else
            {
                if (!RuntimeHelpers.TryGetTypeName(left, out typeName))
                    throw new ArgumentOutOfRangeException("left", "Argument is of non-suported type");
            }

            throw new LuaRuntimeException(ExceptionMessage.OP_TYPE_ERROR, "concatenate", typeName);
        }

        public static object BinaryOpMetamethod(Context context, ExprType op, object left, object right)
        {
            switch (op)
            {
                case ExprType.Add:
                case ExprType.Subtract:
                case ExprType.Multiply:
                case ExprType.Divide:
                case ExprType.Modulo:
                case ExprType.Power:
                    return NumericMetamethod(context, op, left, right);

                case ExprType.GreaterThan:
                case ExprType.GreaterThanOrEqual:
                case ExprType.LessThan:
                case ExprType.LessThanOrEqual:
                    return RelationalMetamethod(context, op, left, right);

                default:
                    throw new ArgumentOutOfRangeException("op");
            }
        }

        public static object NumericMetamethod(Context context, ExprType op, object left, object right)
        {
            var methodName = GetMethodName(op);

            dynamic metamethod = context.GetMetamethod(left, methodName) ?? context.GetMetamethod(right, methodName);
            if (metamethod != null)
                return metamethod(left, right);

            string typeName;
            if (context.GlobalLibrary.ToNumber(left) == null)
            {
                if (!RuntimeHelpers.TryGetTypeName(left, out typeName))
                    throw new ArgumentOutOfRangeException("left", "Argument is of non-suported type");
            }

            Debug.Assert(context.GlobalLibrary.ToNumber(right) != null);
            if (!RuntimeHelpers.TryGetTypeName(right, out typeName))
                throw new ArgumentOutOfRangeException("right", "Argument is of non-suported type");

            throw new LuaRuntimeException(ExceptionMessage.OP_TYPE_ERROR, "perform arithmetic on", typeName);
        }

        public static object RelationalMetamethod(Context context, ExprType op, object left, object right)
        {
            if (left.GetType() != right.GetType())
                return false;

            // There are no metamethods for 'a > b' and 'a >= b' so they are translated to 'b < a' and 'b <= a' respectively
            var invert = op == ExprType.GreaterThan || op == ExprType.GreaterThanOrEqual;

            dynamic metamethod = GetRelationalMetamethod(context, op, left, right);

            if (metamethod != null)
                return invert ? metamethod(right, left) : metamethod(left, right);

            // In the absence of a '<=' metamethod, try '<', 'a <= b' is translated to 'not (b < a)'
            if (op != ExprType.LessThanOrEqual && op != ExprType.GreaterThanOrEqual)
                return false;

            metamethod = GetRelationalMetamethod(context, ExprType.LessThan, left, right);
            if (metamethod != null)
                return invert ? LuaOps.Not(metamethod(right, left)) : LuaOps.Not(metamethod(left, right));

            string leftTypeName, rightTypeName;
            if (!RuntimeHelpers.TryGetTypeName(left, out leftTypeName))
                throw new ArgumentOutOfRangeException("left", "Argument is of non-suported type");
            if (!RuntimeHelpers.TryGetTypeName(right, out rightTypeName))
                throw new ArgumentOutOfRangeException("right", "Argument is of non-suported type");

            if (leftTypeName == rightTypeName)
                throw new LuaRuntimeException(ExceptionMessage.OP_TYPE_TWO_ERROR, "compare", leftTypeName);
            throw new LuaRuntimeException(ExceptionMessage.OP_TYPE_WITH_ERROR, "compare", leftTypeName, rightTypeName);
        }

        static dynamic GetRelationalMetamethod(Context context, ExprType op, object left, object right)
        {
            var methodName = GetMethodName(op);
            dynamic metamethodLeft = context.GetMetamethod(left, methodName);
            dynamic metamethodRight = context.GetMetamethod(right, methodName);
            return metamethodLeft != metamethodRight ? null : metamethodLeft;
        }

        static string GetMethodName(ExprType op)
        {
            string methodName;
            return Constant.METAMETHODS.TryGetValue(op, out methodName) ? methodName : null;
        }

        public static void VarargsAssign(IRuntimeVariables variables, object[] values)
        {
            var variablesArray = VarargsAssign(variables.Count, values);
            for (var i = 0; i < variables.Count; i++)
                variables[i] = variablesArray[i];
        }

        public static object[] VarargsAssign(int numVariables, object[] values)
        {
            var variables = new object[numVariables];
            AssignValuesToVariables(variables, values, 0);
            return variables;
        }

        static void AssignValuesToVariables(object[] variables, IList<object> values, int varCount)
        {
            for (var valueCount = 0; valueCount < values.Count - 1 && varCount < variables.Length; valueCount++, varCount++)
                variables[varCount] = values[valueCount];

            if (varCount < variables.Length)
            {
                Varargs varargs;
                if ((varargs = values.Last() as Varargs) != null)
                    AssignValuesToVariables(variables, varargs, varCount);
                else
                    variables[varCount] = values.Last();
            }
        }
    }
}
