using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IronLua
{
    static class ExceptionMessage
    {
        public const string UNEXPECTED_EOF = "Unexpected end of file";
        public const string UNEXPECTED_EOS = "Unexpected end of string";
        public const string UNEXPECTED_CHAR = "Unexpected '{0}'";
        public const string UNKNOWN_PUNCTUATION = "Unknown punctuation '{0}'";
        public const string INVALID_LONG_STRING_DELIMTER = "Invalid long string delimter '{0}'";
        public const string UNEXPECTED_SYMBOL = "Unexpected symbol '{0}'";
        public const string EXPECTED_SYMBOL = "Unexpected symbol '{0}', expected '{1}'";
        public const string MALFORMED_NUMBER = "Malformed number '{0}'";
        public const string AMBIGUOUS_SYNTAX_FUNCTION_CALL = "Ambiguous syntax (function call or new statement)";
    }
}
