using System;
using System.Globalization;

namespace IronLua.Library
{
    static class NumberUtil
    {
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
            string hexIntPart = null;
            string hexFracPart = null;
            string exponentPart = null;

            var fields = number.Split('p', 'P'); // split between mantissa and exponent
            if (fields.Length >= 1)
            {
                var parts = fields[0].Split('.'); // split on integer and fraction parts
                if (parts.Length >= 1)
                    hexIntPart = parts[0];
                if (parts.Length == 2)
                    hexFracPart = parts[1];
            }
            if (fields.Length == 2)
            {
                exponentPart = fields[1];
                
                if (!exponentAllowed)
                {
                    result = 0;
                    return false;
                }
            }

            ulong hexIntNumber = 0;
            ulong hexFracNumber = 0;
            long exponentNumber = 0;

            bool successful = true;

            if (!String.IsNullOrEmpty(hexIntPart))
                successful &= UInt64.TryParse(hexIntPart, NumberStyles.AllowHexSpecifier, Constant.INVARIANT_CULTURE, out hexIntNumber);

            if (!String.IsNullOrEmpty(hexFracPart))
                successful &= UInt64.TryParse(hexFracPart, NumberStyles.AllowHexSpecifier, Constant.INVARIANT_CULTURE, out hexFracNumber);

            if (!String.IsNullOrEmpty(exponentPart))
                successful &= Int64.TryParse(exponentPart, NumberStyles.AllowLeadingSign, Constant.INVARIANT_CULTURE, out exponentNumber);

            // TODO: what is the formula. Looks like doing shift left/right based on exponent
            // TODO: assert(0x4P-2 == 1) -- shift right 2

            // TODO: fraction part has not been implmented!

            result = hexIntNumber * Math.Pow(2.0, exponentNumber); 
            return successful;
        }
    }
}
