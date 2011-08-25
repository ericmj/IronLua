using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using IronLua.Runtime;

namespace IronLua.Library
{
    static class Global
    {
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

        public static void Setup(LuaTable globals)
        {
            globals.SetValue("tonumber", (Func<string, int?, double>)ToNumber);
        }
    }
}
