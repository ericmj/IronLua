using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using IronLua.Util;

namespace IronLua.Library
{
    static class StringFormatter
    {
        static readonly  Dictionary<char, FormatSpecifier> formatSpecifiers =
            new Dictionary<char, FormatSpecifier>
                {
                    {'c', FormatSpecifier.Character},
                    {'d', FormatSpecifier.SignedDecimalInteger},
                    {'i', FormatSpecifier.SignedDecimalInteger},
                    {'u', FormatSpecifier.UnsignedDecimalInteger},
                    {'e', FormatSpecifier.ScientificNotationLower},
                    {'E', FormatSpecifier.ScientificNotationUpper},
                    {'f', FormatSpecifier.DecimalFloatingPoint},
                    {'g', FormatSpecifier.ShorterScientificOrFloatingLower},
                    {'G', FormatSpecifier.ShorterScientificOrFloatingUpper},
                    {'o', FormatSpecifier.OctalInteger},
                    {'s', FormatSpecifier.String},
                    {'u', FormatSpecifier.SignedDecimalInteger},
                    {'x', FormatSpecifier.HexadecimalIntegerLower},
                    {'X', FormatSpecifier.HexadecimalIntegerUpper}
                };


        public static string Format(string format, object[] values)
        {
            var sb = new StringBuilder(format.Length);
            var valueIndex = 0;
            var tagCount = 1;

            for (var i=0; i<format.Length;)
            {
                var current = format[i];
                var next = i + 1 < format.Length ? format[i + 1] : '\0';

                if (current == '%' && next == '%')
                {
                    sb.Append('%');
                    i+=2;
                }
                else if (current == '%')
                {
                    var tag = ParseTag(format, tagCount++, ref i);
                    var objectValue = values[valueIndex++];
                    string stringValue;

                    if ((stringValue = objectValue as string) != null)
                        sb.Append(tag.Format(stringValue));
                    else if (objectValue is double)
                        sb.Append(tag.Format((double)objectValue));
                    else
                        sb.Append(tag.Format(objectValue.ToString()));
                }
                else
                {
                    sb.Append(current);
                    i++;
                }
            }

            return sb.ToString();
        }

        static FormatTag ParseTag(string format, int tagCount, ref int i)
        {
            i++;
            var tag = new FormatTag(tagCount);

            ParseFlags(tag, format, ref i);
            ParseWidth(tag, format, ref i);
            ParsePrecision(tag, format, ref i);
            ParseSpecifier(tag, format, ref i);

            return tag;
        }

        static void ParseFlags(FormatTag tag, string format, ref int i)
        {
            while (true)
            {
                switch (format[i])
                {
                    case '-':
                        tag.LeftJustify = true;
                        break;
                    case '+':
                        tag.ForceSignPrecedence = true;
                        break;
                    case '#':
                        tag.Hash = true;
                        break;
                    case ' ':
                        tag.PadSignWithSpace = true;
                        break;
                    case '0':
                        tag.PadWithZeros = true;
                        break;
                    default:
                        return;
                }
                i++;
            }
        }

        static void ParseWidth(FormatTag tag, string format, ref int i)
        {
            while (format[i].IsDecimal())
            {
                tag.Width *= 10;
                tag.Width += format[i] - '0';
                i++;
            }
        }

        static void ParsePrecision(FormatTag tag, string format, ref int i)
        {
            if (format[i] != '.')
                return;
            i++;
            tag.Precision = 0;
            
            while (format[i].IsDecimal())
            {
                tag.Precision *= 10;
                tag.Precision += format[i] - '0';
                i++;
            }
        }

        static void ParseSpecifier(FormatTag tag, string format, ref int i)
        {
            FormatSpecifier specifier;
            if (!formatSpecifiers.TryGetValue(format[i], out specifier))
                throw new LuaRuntimeException(ExceptionMessage.STRING_FORMAT_INVALID_OPTION, format);

            tag.Specifier = specifier;
            i++;
        }


        class FormatTag
        {
            public FormatSpecifier Specifier { get; set; }

            // Flags
            public bool LeftJustify { get; set; }         // '-'
            public bool ForceSignPrecedence { get; set; } // '+'
            public bool PadSignWithSpace { get; set; }    // ' '
            public bool Hash { get; set; }                // '#'
            public bool PadWithZeros { get; set; }        // '0'

            public int Width { get; set; }
            public int Precision { get; set; }

            readonly int Count;
            readonly StringBuilder sb;

            public FormatTag(int count)
            {
                Count = count;
                sb = new StringBuilder();
                Precision = -1;
            }

            public string Format(double value)
            {
                sb.Clear();

                switch (Specifier)
                {
                    case FormatSpecifier.Character:
                        FormatChar(value);
                        break;
                    case FormatSpecifier.SignedDecimalInteger:
                        FormatSignDecInt(value);
                        break;
                    case FormatSpecifier.UnsignedDecimalInteger:
                        FormatUnsignDecInt(value);
                        break;
                    case FormatSpecifier.ScientificNotationLower:
                        FormatScienctific(value, false);
                        break;
                    case FormatSpecifier.ScientificNotationUpper:
                        FormatScienctific(value, true);
                        break;
                    case FormatSpecifier.DecimalFloatingPoint:
                        FormatFloating(value);
                        break;
                    case FormatSpecifier.ShorterScientificOrFloatingLower:
                        FormatShorter(value, false);
                        break;
                    case FormatSpecifier.ShorterScientificOrFloatingUpper:
                        FormatShorter(value, true);
                        break;
                    case FormatSpecifier.OctalInteger:
                        FormatOctInt(value);
                        break;
                    case FormatSpecifier.HexadecimalIntegerLower:
                        FormatHexInt(value, false);
                        break;
                    case FormatSpecifier.HexadecimalIntegerUpper:
                        FormatHexInt(value, true);
                        break;
                    case FormatSpecifier.String:
                        FormatString(value.ToString());
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                return sb.ToString();
            }

            public string Format(string value)
            {
                sb.Clear();
                if (Specifier != FormatSpecifier.String)
                    throw new LuaRuntimeException(ExceptionMessage.INVOKE_BAD_ARGUMENT_GOT, Count + 1, "number", "string");
                FormatString(value);
                return sb.ToString();
            }

            void FormatString(string value)
            {
                // Truncate
                if (Precision != -1 && value.Length > Precision)
                    value = value.Substring(0, Precision);

                sb.Append(value);
                Pad();
            }

            void FormatChar(double value)
            {
                var c = (char)value;
                sb.Append(c.ToString());
                Pad();
            }

            void FormatSignDecInt(double value)
            {
                var str = Math.Abs((int)value).ToString();
                sb.Append(str);
                AddMinimumDigits();
                AddSign(value);
                Pad();
            }

            void FormatUnsignDecInt(double value)
            {
                var str = Math.Abs((uint)value).ToString();
                sb.Append(str);
                AddMinimumDigits();
                Pad();
            }

            void FormatScienctific(double value, bool upperCase)
            {
                string formatString = (upperCase ? "E" : "e") + (Precision == -1 ? 6 : Precision);
                var str = Math.Abs(value).ToString(formatString, CultureInfo.InvariantCulture);
                sb.Append(str);
                AddDecimalPoint(str);
                AddSign(value);
                Pad();
            }

            void FormatFloating(double value)
            {
                var formatString = "N" + (Precision == -1 ? 6 : Precision);
                var str = Math.Abs(value).ToString(formatString, CultureInfo.InvariantCulture);

                sb.Append(str);
                AddDecimalPoint(str);
                AddSign(value);
                Pad();
            }

            void FormatShorter(double value, bool upperCase)
            {
                FormatFloating(value);
                var floatingStr = sb.ToString();
                FormatScienctific(value, upperCase);

                if (floatingStr.Length < sb.Length)
                {
                    sb.Clear().Append(floatingStr);
                }
            }

            void FormatOctInt(double value)
            {
                var str = Convert.ToString(Math.Abs((uint)value), 8);
                if (Hash) sb.Append('0');

                sb.Append(str);
                AddMinimumDigits();
                Pad();
            }

            void FormatHexInt(double value, bool upperCase)
            {
                var formatString = upperCase ? "X" : "x";
                var str = Math.Abs((uint)value).ToString(formatString);

                if (Hash)
                    sb.Append(upperCase ? "0X" : "0x");

                sb.Append(str);
                AddMinimumDigits();
                Pad();
            }

            void AddDecimalPoint(string str)
            {
                // We could do a Math.Truncate(value) == value to check if we have added a decimal point instead
                // of the costly str.Contains('.') but i'm not sure how well that will work when floating point
                // equality comparsions are unreliable
                if (Hash && Precision == 0 && str.Contains('.'))
                    sb.Append('.');
            }

            void AddMinimumDigits()
            {
                if (sb.Length < Precision)
                    sb.Insert(0, "0", Precision - sb.Length);
            }

            void AddSign(double value)
            {
                if (value < 0)
                {
                    sb.Insert(0, '-');
                }
                else
                {
                    if (ForceSignPrecedence)
                        sb.Insert(0, '+');
                    else if (PadSignWithSpace)
                        sb.Insert(0, ' ');
                }
            }

            void Pad()
            {
                int paddingCount = Width - sb.Length;
                if (paddingCount <= 0)
                    return;

                if (LeftJustify)
                {
                    for (int i = 0; i < paddingCount; i++)
                        sb.Append(' ');
                }
                else
                {
                    var padding = (PadWithZeros ? '0' : ' ').ToString();
                    sb.Insert(0, padding, paddingCount);
                }
            }
        }

        enum FormatSpecifier
        {
            Character,
            SignedDecimalInteger,
            UnsignedDecimalInteger,
            ScientificNotationLower,
            ScientificNotationUpper,
            DecimalFloatingPoint,
            ShorterScientificOrFloatingLower,
            ShorterScientificOrFloatingUpper,
            OctalInteger,
            String,
            HexadecimalIntegerLower,
            HexadecimalIntegerUpper
        }
    }
}