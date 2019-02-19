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
            return Double.TryParse(number, DECIMAL_NUMBER_STYLE, CultureInfo.InvariantCulture, out result);
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
            
            ulong integer = 0;
            double fraction = 0;
            long exponent = 0;

            bool successful = true;

            if (!String.IsNullOrEmpty(hexIntPart)) 
                successful &= UInt64.TryParse(hexIntPart, NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out integer);

            if (!String.IsNullOrEmpty(hexFracPart))
            {
                ulong value;
                successful &= UInt64.TryParse(hexFracPart, NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out value);
                fraction = value / Math.Pow(16.0, hexFracPart.Length);
            }

            if (!String.IsNullOrEmpty(exponentPart))
                successful &= Int64.TryParse(exponentPart, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out exponent);

            result = (integer + fraction) * Math.Pow(2.0, exponent); 
            return successful;
        }
    }
}
