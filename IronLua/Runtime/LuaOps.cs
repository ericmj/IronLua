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

            throw new LuaRuntimeException(ExceptionMessage.OP_TYPE_ERROR, "get length of", BaseLibrary.Type(obj));
        }

        public static object UnaryMinusMetamethod(Context context, object obj)
        {
            dynamic metamethod = context.GetMetamethod(obj, Constant.UNARYMINUS_METAMETHOD);
            if (metamethod != null)
                return metamethod(obj);

            throw new LuaRuntimeException(ExceptionMessage.OP_TYPE_ERROR, "perform arithmetic on", BaseLibrary.Type(obj));
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

            throw new LuaRuntimeException(ExceptionMessage.OP_TYPE_ERROR, "index", BaseLibrary.Type(obj));
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

            throw new LuaRuntimeException(ExceptionMessage.OP_TYPE_ERROR, "index", BaseLibrary.Type(obj));
        }

        public static object CallMetamethod(Context context, object obj, object[] args)
        {
            dynamic metamethod = context.GetMetamethod(obj, Constant.CALL_METAMETHOD);
            if (metamethod != null)
            {
                var array = new object[args.Length + 1];
                array[0] = obj;
                Array.Copy(args, 0, array, 1, args.Length);
                return context.GetDynamicCall1()(metamethod, new Varargs(array));
            }

            throw new LuaRuntimeException(ExceptionMessage.OP_TYPE_ERROR, "call", BaseLibrary.Type(obj));
        }

        public static object ConcatMetamethod(Context context, object left, object right)
        {
            dynamic metamethod = context.GetMetamethod(left, Constant.CONCAT_METAMETHOD) ??
                                 context.GetMetamethod(right, Constant.CONCAT_METAMETHOD);
            if (metamethod != null)
                return metamethod(left, right);

            var typeName = left is string ? BaseLibrary.Type(left) : BaseLibrary.Type(right);
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

            var typeName = BaseLibrary.Type(BaseLibrary.ToNumber(left) == null ? left : right);
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

            var leftTypeName = BaseLibrary.Type(left);
            var rightTypeName = BaseLibrary.Type(right);

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
            Varargs varargs;

            for (var valueCount = 0; valueCount < values.Count && varCount < variables.Length; valueCount++, varCount++)
            {
                var value = values[valueCount];
                if ((varargs = value as Varargs) != null)
                {
                    // Expand varargs if it's the last value otherwise just take the first value in varargs
                    if (valueCount + 1 == values.Count)
                        AssignValuesToVariables(variables, varargs, varCount);
                    else
                        variables[varCount] = varargs.First();
                }
                else
                {
                    variables[varCount] = value;
                }
            }
        }
    }
}
