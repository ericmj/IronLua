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

        // TODO: Test this
        // NOTE: Assignment to IRuntimeVariables prolly doesn't work. So we can generalize this for global assignments
        //       too. Allocate a object[] in Generator assign to the array here and then assign from the array to
        //       the parameters in generator.
        public static void VarargsAssign(IRuntimeVariables variables, object[] values)
        {
            int varCount = 0;
            for (int valueCount = 0; valueCount < values.Length && varCount < variables.Count; valueCount++)
            {
                var value = values[valueCount];
                Varargs varargs;

                if ((varargs = value as Varargs) != null)
                {
                    for (int varargsCount = 0;
                         varargsCount < varargs.Count && varCount < variables.Count;
                         varargsCount++, varCount++)
                        variables[varCount] = varargs[varargsCount];
                }
                else
                {
                    variables[varCount++] = value;
                }
            }
        }
    }
}
