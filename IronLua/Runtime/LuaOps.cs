using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Text;

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

            return GetLengthMetamethod(context, obj);
        }

        public static object GetLengthMetamethod(Context context, object obj)
        {
            dynamic metamethod = context.GetMetamethod(Constant.LENGTH_METAMETHOD, obj);
            if (metamethod == null)
                throw new Exception(); // TODO
            return metamethod(obj);
        }

        public static object GetConcatMetamethod(Context context, object left, object right)
        {
            dynamic metamethod = context.GetMetamethod(Constant.CONCAT_METAMETHOD, left) ??
                                 context.GetMetamethod(Constant.CONCAT_METAMETHOD, right);
            if (metamethod == null)
                throw new Exception(); // TODO
            return metamethod(left, right);
        }

        public static object GetBinaryOpMetamethod(Context context, ExpressionType op, object left, object right)
        {
            switch (op)
            {
                case ExpressionType.Add:
                case ExpressionType.Subtract:
                case ExpressionType.Multiply:
                case ExpressionType.Divide:
                case ExpressionType.Modulo:
                case ExpressionType.Power:
                    return NumericMetamethod(context, op, left, right);

                case ExpressionType.GreaterThan:
                case ExpressionType.GreaterThanOrEqual:
                case ExpressionType.LessThan:
                case ExpressionType.LessThanOrEqual:
                    return RelationalMetamethod(context, op, left, right);

                default:
                    throw new Exception(); // TODO
            }
        }

        static object NumericMetamethod(Context context, ExpressionType op, object left, object right)
        {
            var methodName = GetMethodName(op);

            dynamic metamethod = context.GetMetamethod( methodName, left) ?? context.GetMetamethod(methodName, right);
            if (metamethod == null)
                throw new Exception(); // TODO
            return metamethod(left, right);
        }

        static object RelationalMetamethod(Context context, ExpressionType op, object left, object right)
        {
            if (left.GetType() != right.GetType())
                return false;

            // There are no metamethods for 'a > b' and 'a >= b' so they are translated to 'b < a' and 'b <= a' respectively
            bool invert = op == ExpressionType.GreaterThan || op == ExpressionType.GreaterThanOrEqual;

            dynamic metamethod = GetRelationalMetamethod(context, op, left, right);

            if (metamethod == null)
            {
                // In the absence of a '<=' metamethod, try '<', 'a <= b' is translated to 'not (b < a)'
                if (op != ExpressionType.LessThanOrEqual && op != ExpressionType.GreaterThanOrEqual)
                    return false;

                metamethod = GetRelationalMetamethod(context, ExpressionType.LessThan, left, right);
                if (metamethod == null)
                    return false;

                // TODO: Remove Global.Not and output a dynamic unary expression with ExprType.Not
                return invert ? LuaOps.Not(metamethod(right, left)) : LuaOps.Not(metamethod(left, right));
            }

            return invert ? metamethod(right, left) : metamethod(left, right);
        }

        static dynamic GetRelationalMetamethod(Context context, ExpressionType op, object left, object right)
        {
            var methodName = GetMethodName(op);
            dynamic metamethodLeft = context.GetMetamethod(methodName, left);
            dynamic metamethodRight = context.GetMetamethod(methodName, right);
            return metamethodLeft != metamethodRight ? null : metamethodLeft;
        }

        static string GetMethodName(ExpressionType op)
        {
            string methodName;
            if (!Constant.METAMETHODS.TryGetValue(op, out methodName))
                throw new Exception(); // TODO
            return methodName;
        }

        public static void VarargsAssign(IRuntimeVariables variables, object[] values)
        {
            var variablesArray = VarargsAssign(variables.Count, values);
            for (int i = 0; i < variables.Count; i++)
                variables[i] = variablesArray[i];
        }

        public static object[] VarargsAssign(int numVariables, object[] values)
        {
            var variables = new object[numVariables];

            int varCount = 0;
            for (int valueCount = 0; valueCount < values.Length && varCount < variables.Length; valueCount++)
            {
                var value = values[valueCount];
                Varargs varargs;

                // TODO: Fix! Should only expand last varargs, other varargs should only call .First()
                if ((varargs = value as Varargs) != null)
                    AssignVarargsToVariables(variables, varargs, ref varCount);
                else
                    variables[varCount++] = value;
            }

            return variables;
        }

        static void AssignVarargsToVariables(object[] variables, Varargs varargs, ref int varCount)
        {
            for (int varargsCount = 0;
                 varargsCount < varargs.Count && varCount < variables.Length;
                 varargsCount++, varCount++)
            {
                Varargs varargs2;

                if (varargsCount + 1 == varargs.Count && (varargs2 = varargs[varargsCount] as Varargs) != null)
                    AssignVarargsToVariables(variables, varargs2, ref varCount);
                else
                    variables[varCount] = varargs[varargsCount];
            }
        }
    }
}
