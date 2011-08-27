using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using IronLua.Util;

namespace IronLua.Library
{
    static class LuaString
    {
        public static double[] Byte(string str, int? i = null, int? j = null)
        {
            char[] chars;
            if (!i.HasValue)
                chars = str.ToCharArray();
            else if (!j.HasValue)
                chars = str.Substring(i.Value).ToCharArray();
            else
                chars = str.Substring(i.Value, j.Value).ToCharArray();

            return chars.Select(c => (double)c).ToArray();
        }

        public static string Char(params double[] varargs)
        {
            var sb = new StringBuilder(varargs.Length);
            foreach (var arg in varargs)
                sb.Append((char) arg);
            return sb.ToString();
        }

        public static string Dump(object function)
        {
            throw new NotImplementedException();
        }

        public static object[] Find(string str, string pattern, int? init = 1, bool? plain = false)
        {
            if (plain.HasValue && plain.Value == true && init.HasValue)
            {
                int index = str.Substring(init.Value).IndexOf(pattern);
                return index != -1 ? new object[] {index, index+pattern.Length} : null;
            }
            throw new NotImplementedException();
        }

        public static string Format(string format, params object[] varargs)
        {
            // TODO
            return String.Format(format, varargs);
        }
    }

    class FormatParser
    {
        static Dictionary<char, FormatSpecifier> formatSpecifiers =
            new Dictionary<char, FormatSpecifier>
                {
                    {'c', FormatSpecifier.Character},
                    {'d', FormatSpecifier.DecimalInteger},
                    {'i', FormatSpecifier.DecimalInteger},
                    {'e', FormatSpecifier.ScientificNotationLower},
                    {'E', FormatSpecifier.ScientificNotationUpper},
                    {'f', FormatSpecifier.DecimalFloatingPoint},
                    {'g', FormatSpecifier.ShorterScientificOrFloatingLower},
                    {'G', FormatSpecifier.ShorterScientificOrFloatingUpper},
                    {'o', FormatSpecifier.OctalInteger},
                    {'s', FormatSpecifier.String},
                    {'u', FormatSpecifier.DecimalInteger},
                    {'x', FormatSpecifier.HexadecimalIntegerLower},
                    {'X', FormatSpecifier.HexadecimalIntegerUpper}
                };


        public void Format(string format, object[] values)
        {
            var sb = new StringBuilder(format.Length);
            int valueIndex = 0;

            for (int i=0; i<format.Length; i++)
            {
                char current = format[i];
                char next = i + 1 < format.Length ? format[i + 1] : '\0';

                if (current == '%' && next == '%')
                {
                    sb.Append('%');
                    i+=2;
                }
                else if (current == '%')
                {
                    var tag = ParseTag(sb, format, ref i);
                    object objectValue = values[valueIndex++];
                    string stringValue;

                    if ((stringValue = objectValue as string) != null)
                        tag.Append(stringValue);
                    else if (objectValue is double)
                        tag.Append((double)objectValue);
                    else
                        tag.Append(objectValue.ToString());
                }
                else
                {
                    sb.Append(current);
                }
            }
        }

        FormatTag ParseTag(StringBuilder sb, string format, ref int i)
        {
            i++;
            var tag = new FormatTag(sb);

            ParseFlags(tag, format, ref i);
            ParseWidth(tag, format, ref i);
            ParsePrecision(tag, format, ref i);
            ParseSpecifier(tag, format, ref i);

            return tag;
        }

        void ParseFlags(FormatTag tag, string format, ref int i)
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

        void ParseWidth(FormatTag tag, string format, ref int i)
        {
            while (format[i].IsDecimal())
            {
                tag.Width *= 10;
                tag.Width += format[i] - '0';
                i++;
            }
        }

        void ParsePrecision(FormatTag tag, string format, ref int i)
        {
            if (format[i] != '.')
                return;
            i++;

            if (!format[i].IsDecimal())
                tag.Precision = 0;
            
            while (format[i].IsDecimal())
            {
                tag.Precision *= 10;
                tag.Precision += format[i] - '0';
                i++;
            }
        }

        void ParseSpecifier(FormatTag tag, string format, ref int i)
        {
            FormatSpecifier specifier;
            if (!formatSpecifiers.TryGetValue(format[i], out specifier))
                throw new Exception(); // TODO

            tag.Specifier = specifier;
            i++;
        }
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

        StringBuilder sb;

        public FormatTag(StringBuilder sb)
        {
            this.sb = sb;
            Precision = -1;
        }

        public void Append(double value)
        {
            switch (Specifier)
            {
                case FormatSpecifier.Character:
                    FormatChar(value);
                    break;
                case FormatSpecifier.DecimalInteger:
                    FormatDecInt(value);
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
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public void Append(string value)
        {
            if (Specifier != FormatSpecifier.String)
                throw new Exception(); // TODO

            // Truncate
            if (Precision != -1 && value.Length > Precision)
                value = value.Substring(0, Precision);

            sb.Append(value);
            Pad();
        }

        void FormatChar(double value)
        {
            char c = (char)value;
            sb.Append(c.ToString());
            Pad();
        }

        void FormatDecInt(double value)
        {
            string str = ((long) value).ToString();
            AddMinimumDigits(str);
            AddSign(value);
            Pad();
        }

        void FormatScienctific(double value, bool upperCase)
        {
            throw new NotImplementedException();
        }

        void FormatFloating(double value)
        {
            throw new NotImplementedException();
        }

        void FormatShorter(double value, bool upperCase)
        {
            throw new NotImplementedException();
        }

        void FormatOctInt(double value)
        {
            throw new NotImplementedException();
        }

        void FormatHexInt(double value, bool upperCase)
        {
            throw new NotImplementedException();
        }

        void Pad()
        {
            int paddingCount = Width - sb.Length;
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

        void AddMinimumDigits(string str)
        {
            if (str.Length < Precision)
                sb.Insert(0, str, Precision - str.Length);
        }

        void AddSign(double value)
        {
            if (value > 0)
            {
                if (ForceSignPrecedence)
                    sb.Insert(0, '+');
                else if (PadSignWithSpace)
                    sb.Insert(0, ' ');
            }
        }
    }

    enum FormatSpecifier
    {
        Character,
        DecimalInteger,
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
