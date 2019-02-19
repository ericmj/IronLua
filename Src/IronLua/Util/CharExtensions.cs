namespace IronLua.Util
{
    static class CharExtensions
    {
        public static bool IsDecimal(this char c)
        {
            return c >= '0' && c <= '9';
        }

        public static bool IsHex(this char c)
        {
            return
                (c >= '0' && c <= '9') ||
                (c >= 'a' && c <= 'f') ||
                (c >= 'A' && c <= 'F');
        }

        public static bool IsPunctuation(this char c)
        {
            switch (c)
            {
                case '+':
                case '-':
                case '*':
                case '/':
                case '%':
                case '^':
                case '#':
                case '~':
                case '<':
                case '>':
                case '=':
                case '(':
                case ')':
                case '{':
                case '}':
                case '[':
                case ']':
                case ';':
                case ':':
                case ',':
                case '.':
                    return true;
                default:
                    return false;
            }
        }

        public static bool IsIdentifierStart(this char c)
        {
            return
                (c >= 'a' && c <= 'z') ||
                (c >= 'A' && c <= 'Z') ||
                (c == '_');
        }

        public static bool IsIdentifier(this char c)
        {
            return
                (c >= 'a' && c <= 'z') ||
                (c >= 'A' && c <= 'Z') ||
                (c >= '0' && c <= '9') ||
                (c == '_');
        }
    }
}
