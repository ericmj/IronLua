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


        public void Parse(string format)
        {
            var newFormat = new StringBuilder(format.Length);
            var formatTags = new List<FormatTag>();

            for (int i=0; i<format.Length; i++)
            {
                char current = format[i];
                char next = i + 1 < format.Length ? format[i + 1] : '\0';

                if (current == '%' && next == '%')
                {
                    newFormat.Append('%');
                    i+=2;
                }
                else if (current == '%')
                {
                    newFormat.AppendFormat("{{{0}}}", formatTags.Count);
                    formatTags.Add(ParseTag(format, ref i));
                }
                else
                {
                    newFormat.Append(current);
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
            if (!format[i].IsDecimal())
                return;

            tag.Width = format[i] - '0';
            i++;
        }

        void ParsePrecision(FormatTag tag, string format, ref int i)
        {
            if (format[i] != '.')
                return;
            i++;

            tag.Precision = !format[i].IsDecimal() ? 0 : format[i] - '0';
            i++;
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
        public bool LeftJustify { get; set; }
        public bool ForceSignPrecedence { get; set; }
        public bool PadSignWithSpace { get; set; }
        public bool Hash { get; set; }
        public bool PadWithZeros { get; set; }

        public int Width { get; set; }
        public int Precision { get; set; }

        public FormatTag()
        {
            LeftJustify = false;
            ForceSignPrecedence = false;
            PadSignWithSpace = false;
            Hash = false;
            PadWithZeros = false;
            Width = 0;
            Precision = 1; // If period is specified without an explicit value for precision, 0 is assumed
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
