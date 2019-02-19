namespace IronLua
{
    static class ExceptionMessage
    {
        // Syntax
        public const string UNEXPECTED_EOF = "Unexpected end of file";
        public const string UNEXPECTED_EOS = "Unexpected end of string";
        public const string UNEXPECTED_CHAR = "Unexpected '{0}'";
        public const string UNKNOWN_PUNCTUATION = "Unknown punctuation '{0}'";
        public const string INVALID_LONG_STRING_DELIMTER = "Invalid long string delimter '{0}'";
        public const string UNEXPECTED_SYMBOL = "Unexpected symbol '{0}'";
        public const string EXPECTED_SYMBOL = "Unexpected symbol '{0}', expected '{1}'";
        public const string MALFORMED_NUMBER = "Malformed number '{0}'";
        public const string AMBIGUOUS_SYNTAX_FUNCTION_CALL = "Ambiguous syntax (function call or new statement)";

        // Runtime
        public const string FOR_VALUE_NOT_NUMBER = "For loop {0} must be a number";
        public const string OP_TYPE_ERROR = "Attempt to {0} a {1} value";
        public const string OP_TYPE_WITH_ERROR = "Attempt to {0} {1} with {2}";
        public const string OP_TYPE_TWO_ERROR = "Attempt to {0} two {1} values";
        // TODO: Add "to '...'" when we implement call stacks
        public const string INVOKE_BAD_ARGUMENT = "Bad argument #{0} ({1})";
        public const string INVOKE_BAD_ARGUMENT_EXPECTED = "Bad argument #{0} ({1} expected)";
        public const string INVOKE_BAD_ARGUMENT_GOT = "Bad argument #{0} ({1} expected, got {2})";
            
        // Library
        //public const string BAD_ARGUMENT_INVALID_OPTION = "Bad argument #{0} to '{1}' (invalid option '{2}')";
        public const string STRING_FORMAT_INVALID_OPTION = "Invalid option '{0}', to 'format'";
        public const string FUNCTION_NOT_IMPLEMENTED = "Function '{0}' not implemented";
        public const string PROTECTED_METATABLE = "Cannot change a protected metatable";
    }
}
