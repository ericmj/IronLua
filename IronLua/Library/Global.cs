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

        public object ToNumber(string str, double @base = 10.0)
        {
            double value = InternalToNumber(str, @base);
            return value == double.NaN ? null : (object)value;
        }

        internal static double InternalToNumber(string str, double @base)
        {
            double result = 0;

            if (@base == 10.0)
            {
                if (str.StartsWith("0x") || str.StartsWith("0X"))
                {
                    if (NumberUtil.TryParseHexNumber(str.Substring(2), true, out result))
                        return result;
                }
                if (NumberUtil.TryParseDecimalNumber(str, out result))
                    return result;
                return double.NaN;
            }

            if (@base == 16.0)
            {
                if (str.StartsWith("0x") || str.StartsWith("0X"))
                {
                    if (NumberUtil.TryParseHexNumber(str.Substring(2), false, out result))
                        return result;
                }
                return double.NaN;
            }

            var reversedStr = new String(str.Reverse().ToArray());
            for (int i = 0; i < reversedStr.Length; i++ )
            {
                int num = AlphaNumericToBase(reversedStr[i]);
                if (num == -1 || num >= @base)
                    return double.NaN;
                result += num * (@base * i + 1);
            }
            return result;
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
            table.SetValue("tonumber", (Func<string, double, object>)ToNumber);
        }
    }
}
