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
                    var tag = ParseTag(format, ref i);
                    object objectValue = values[valueIndex++];
                    string stringValue;

                    if ((stringValue = objectValue as string) != null)
                        tag.Append(sb, stringValue);
                    else if (objectValue is double)
                        tag.Append(sb, (double)objectValue);
                    else
                        tag.Append(sb, objectValue.ToString());
                }
                else
                {
                    sb.Append(current);
                }
            }
        }

        FormatTag ParseTag(string format, ref int i)
        {
            i++;
            var tag = new FormatTag();

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

        public FormatTag()
        {
            Precision = 1;
        }

        public void Append(StringBuilder sb, string value)
        {
            if (Specifier != FormatSpecifier.String)
                throw new Exception(); // TODO

            if (LeftJustify)
            {
                sb.Append(value);
                for (int i = 0; i < Width - value.Length; i++)
                    sb.Append(' ');
            }
            else
            {
                char padding = PadWithZeros ? '0' : ' ';
                for (int i = 0; i < Width - value.Length; i++)
                    sb.Append(padding);
                sb.Append(value);
            }
        }

        public void Append(StringBuilder sb, double value)
        {
            switch (Specifier)
            {
                case FormatSpecifier.Character:
                    FormatChar(sb, value);
                    break;
                case FormatSpecifier.DecimalInteger:
                    FormatDecInt(sb, value);
                    break;
                case FormatSpecifier.ScientificNotationLower:
                    FormatScienctific(sb, value, false);
                    break;
                case FormatSpecifier.ScientificNotationUpper:
                    FormatScienctific(sb, value, true);
                    break;
                case FormatSpecifier.DecimalFloatingPoint:
                    FormatFloating(sb, value);
                    break;
                case FormatSpecifier.ShorterScientificOrFloatingLower:
                    FormatShorter(sb, value, false);
                    break;
                case FormatSpecifier.ShorterScientificOrFloatingUpper:
                    FormatShorter(sb, value, true);
                    break;
                case FormatSpecifier.OctalInteger:
                    FormatOctInt(sb, value);
                    break;
                case FormatSpecifier.HexadecimalIntegerLower:
                    FormatHexInt(sb, value, false);
                    break;
                case FormatSpecifier.HexadecimalIntegerUpper:
                    FormatHexInt(sb, value, true);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        void FormatChar(StringBuilder sb, double value)
        {
            char c = (char)value;

            if (LeftJustify)
            {
                sb.Append(value);
                for (int i = 0; i < Width; i++)
                    sb.Append(' ');
            }
            else
            {
                char padding = PadWithZeros ? '0' : ' ';
                for (int i = 0; i < Width; i++)
                    sb.Append(padding);
                sb.Append(value);
            }
        }

        void FormatDecInt(StringBuilder sb, double value)
        {
            throw new NotImplementedException();
        }

        void FormatScienctific(StringBuilder sb, double value, bool upperCase)
        {
            throw new NotImplementedException();
        }

        void FormatFloating(StringBuilder sb, double value)
        {
            throw new NotImplementedException();
        }

        void FormatShorter(StringBuilder sb, double value, bool upperCase)
        {
            throw new NotImplementedException();
        }

        void FormatOctInt(StringBuilder sb, double value)
        {
            throw new NotImplementedException();
        }

        void FormatHexInt(StringBuilder sb, double value, bool upperCase)
        {
            throw new NotImplementedException();
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
