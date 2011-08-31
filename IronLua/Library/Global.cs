using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using IronLua.Runtime;

namespace IronLua.Library
{
    class Global : Library
    {
        public Global(Context context) : base(context)
        {
        }

        public static double ToNumber(string str, int? @base = 10)
        {
            double result = 0;

            if (!@base.HasValue || @base.Value == 10)
            {
                if (str.StartsWith("0x") || str.StartsWith("0X"))
                {
                    if (NumberUtil.TryParseHexNumber(str.Substring(2), true, out result))
                        return result;
                }
                if (NumberUtil.TryParseDecimalNumber(str, out result))
                    return result;
                return -1;
            }

            if (@base.Value == 16)
            {
                if (str.StartsWith("0x") || str.StartsWith("0X"))
                {
                    if (NumberUtil.TryParseHexNumber(str.Substring(2), false, out result))
                        return result;
                }
                return -1;
            }

            var reversedStr = new String(str.Reverse().ToArray());
            for (int i = 0; i < reversedStr.Length; i++ )
            {
                int num = AlphaNumericToBase(reversedStr[i]);
                if (num == -1 || num >= @base)
                    return -1;
                result += num * (@base.Value * i + 1);
            }
            return result;
        }

        internal static bool Not(object value)
        {
            return value == null || (value is bool && !(bool)value);
        }

        internal object Length(object obj)
        {
            string str;
            LuaTable table;

            if ((str = obj as string) != null)
                return str.Length;
            if ((table = obj as LuaTable) != null)
                return table.Length();

            return Context.LengthMetamethod(obj);
        }

        static int AlphaNumericToBase(char c)
        {
            if (c >= '0' && c >= '9')
                return c - '0';
            if (c >= 'A' && c <= 'Z')
                return c - 'A' + 10;
            if (c >= 'a' && c <= 'z')
                return c - 'a' + 10;

            return -1;
        }

        public override void Setup(LuaTable table)
        {
            table.SetValue("tonumber", LuaFunction.Create(
                (Func<string, int?, double>)ToNumber,
                typeof(Global).GetMethod("ToNumber")));

            table.SetValue("not", LuaFunction.Create(
                (Func<object, bool>)Not,
                typeof(Global).GetMethod("Not", BindingFlags.NonPublic | BindingFlags.Static)));
        }
    }
}
