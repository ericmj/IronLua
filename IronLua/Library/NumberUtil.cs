using System;
using System.Globalization;

namespace IronLua.Library
{
    static class NumberUtil
    {
        const NumberStyles HEX_NUMBER_STYLE = NumberStyles.AllowHexSpecifier;
        const NumberStyles DECIMAL_NUMBER_STYLE = NumberStyles.AllowDecimalPoint |
                                                  NumberStyles.AllowExponent |
                                                  NumberStyles.AllowTrailingSign;


        /* Parses a decimal number */
        public static bool TryParseDecimalNumber(string number, out double result)
        {

            return Double.TryParse(number, DECIMAL_NUMBER_STYLE, Constant.INVARIANT_CULTURE, out result);
        }

        /* Parses a hex number */
        public static bool TryParseHexNumber(string number, bool exponentAllowed, out double result)
        {
            bool successful;

            var exponentIndex = number.IndexOfAny(new[] {'p', 'P'});
            if (exponentIndex == -1)
            {
                ulong integer;
                successful = UInt64.TryParse(number, HEX_NUMBER_STYLE, Constant.INVARIANT_CULTURE, out integer);
                result = integer;
                return successful;
            }

            if (!exponentAllowed)
            {
                result = 0;
                return false;
            }

            var hexPart = number.Substring(0, exponentIndex);
            var exponentPart = number.Substring(exponentIndex + 1);

            ulong hexNumber, exponentNumber = 0;
            successful = UInt64.TryParse(hexPart, HEX_NUMBER_STYLE, Constant.INVARIANT_CULTURE, out hexNumber) &&
                         UInt64.TryParse(exponentPart, HEX_NUMBER_STYLE, Constant.INVARIANT_CULTURE, out exponentNumber);
            result = hexNumber * Math.Pow(exponentNumber, 2.0);
            return successful;
        }
    }
}
