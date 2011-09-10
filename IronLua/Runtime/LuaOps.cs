using System;
using System.Collections.Generic;
using System.Linq;
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

            return context.LengthMetamethod(obj);
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
