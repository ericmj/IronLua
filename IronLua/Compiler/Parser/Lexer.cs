using System.Collections.Generic;
using System.Text;
using IronLua.Util;

namespace IronLua.Compiler.Parser
{
    class Lexer
    {
        static readonly Dictionary<string, Symbol> keywords =
            new Dictionary<string, Symbol>
                {
                    {"and", Symbol.And},
                    {"break", Symbol.Break},
                    {"do", Symbol.Do},
                    {"else", Symbol.Else},
                    {"elseif", Symbol.Elseif},
                    {"end", Symbol.End},
                    {"false", Symbol.False},
                    {"for", Symbol.For},
                    {"function", Symbol.Function},
                    {"if", Symbol.If},
                    {"in", Symbol.In},
                    {"local", Symbol.Local},
                    {"nil", Symbol.Nil},
                    {"not", Symbol.Not},
                    {"or", Symbol.Or},
                    {"repeat", Symbol.Repeat},
                    {"return", Symbol.Return},
                    {"then", Symbol.Then},
                    {"true", Symbol.True},
                    {"until", Symbol.Until},
                    {"while", Symbol.While}
                };

        static readonly Dictionary<string, Symbol> punctuations =
            new Dictionary<string,Symbol>
                {
                    {"+",   Symbol.Plus},
                    {"-",   Symbol.Minus},
                    {"*",   Symbol.Star},
                    {"/",   Symbol.Slash},
                    {"%",   Symbol.Percent},
                    {"^",   Symbol.Caret},
                    {"#",   Symbol.Hash},
                    {"==",  Symbol.EqualEqual},
                    {"~=",  Symbol.TildeEqual},
                    {"<=",  Symbol.LessEqual},
                    {">=",  Symbol.GreaterEqual},
                    {"<",   Symbol.Less},
                    {">",   Symbol.Greater},
                    {"=",   Symbol.Equal},
                    {"(",   Symbol.LeftParen},
                    {")",   Symbol.RightParen},
                    {"{",   Symbol.LeftBrace},
                    {"}",   Symbol.RightBrace},
                    {"[",   Symbol.LeftBrack},
                    {"]",   Symbol.RightBrack},
                    {";",   Symbol.SemiColon},
                    {":",   Symbol.Colon},
                    {",",   Symbol.Comma},
                    {".",   Symbol.Dot},
                    {"..",  Symbol.DotDot},
                    {"...", Symbol.DotDotDot},
                };

        Input input;

        public Token Last { get; private set; }
        public Token Current { get; private set; }
        public Token Next { get; private set; }

        public Lexer(Input input)
        {
            this.input = input;
            Current = NextToken();
            Next = NextToken();
        }

        public void Consume()
        {
            Last = Current;
            Current = Next;
            Next = NextToken();
        }

        public string ConsumeLexeme()
        {
            var lexeme = Current.Lexeme;
            Consume();
            return lexeme;
        }

        public bool TryConsume(Symbol symbol)
        {
            if (Current.Symbol == symbol)
            {
                Consume();
                return true;
            }
            return false;
        }

        public void Expect(Symbol symbol)
        {
            if (Current.Symbol == symbol)
                Consume();
            else
                throw new CompileException(input, ExceptionMessage.EXPECTED_SYMBOL, Current.Symbol, symbol);
        }

        public string ExpectLexeme(Symbol symbol)
        {
            var lexeme = Current.Lexeme;
            Expect(symbol);
            return lexeme;
        }

        private Token NextToken()
        {
            while (input.CanContinue)
            {
                switch (input.Current)
                {
                    // Whitespace
                    case ' ': case '\t':
                        input.Advance();
                        break;

                    // Newlines
                    case '\r': case '\n':
                        NextLine();
                        break;

                    // String
                    case '\'': case '"':
                        return StringLiteral(input.Current);

                    // Comment or minus
                    case '-':
                        input.StorePosition();
                        input.Advance();

                        if (input.Current != '-')
                            return input.Output(Symbol.Minus);

                        if (input.CanPeek && input.Peek == '[')
                            LongComment();
                        else
                            ShortComment();

                        break;

                    default:
                        // Long string
                        if (input.Current == '[' && input.CanPeek &&
                           (input.Current == '[' || input.Current == '='))
                            return LongStringLiteral();

                        // Hex numeric
                        if (input.Current == '0' && input.CanPeek &&
                           (input.Current == 'X' || input.Current == 'x'))
                            return NumericHexLiteral();

                        // Numeric
                        if (input.Current.IsDecimal() ||
                           (input.Current == '.' && input.CanPeek && input.Peek.IsDecimal()))
                            return NumericLiteral();

                        // Identifier or keyword
                        if (input.Current.IsIdentifierStart())
                            return IdentifierOrKeyword();

                        // Punctuation
                        if (input.Current.IsPunctuation())
                            return Punctuation();

                        throw new CompileException(input, ExceptionMessage.UNEXPECTED_CHAR, input.Current);

                }
            }
            return input.Output(Symbol.Eof);
        }

        /* Identifier or keyword */
        Token IdentifierOrKeyword()
        {
            input.StorePosition();
            input.BufferClear();

            while (input.CanContinue && input.Current.IsIdentifier())
            {
                input.BufferAppend(input.Current);
                input.Advance();
            }

            // Keyword or identifier?
            Symbol symbol;
            if (keywords.TryGetValue(input.Buffer, out symbol))
                return input.Output(symbol);
            return input.OutputBuffer(Symbol.Identifier);
        }

        /* Numeric literal, such as 12345 or 45e+1 */
        Token NumericLiteral()
        {
            input.StorePosition();
            input.BufferClear();

            while (input.CanContinue)
            {
                if (input.Current == 'e' || input.Current == 'E')
                {
                    BufferExponent();
                    break;
                }
                if (input.Current.IsDecimal() || input.Current == '.')
                    input.BufferAppend(input.Current);
                else
                    break;

                input.Advance();
            }

            return input.OutputBuffer(Symbol.Number);
        }

        /* Buffers the exponent part of a numeric literal,
         * such as e+5 p8 e2 */
        void BufferExponent()
        {
            input.BufferAppend(input.Current);
            input.Advance();

            if (input.CanContinue && (input.Current == '-' || input.Current == '+'))
            {
                input.BufferAppend(input.Current);
                input.Advance();
            }

            while (input.CanContinue && input.Current.IsDecimal())
            {
                input.BufferAppend(input.Current);
                input.Advance();
            }
        }

        /* Hex literal, such as 0xFF or 0x10p4
         * Can be malformed, parser handles that */
        Token NumericHexLiteral()
        {
            input.StorePosition();
            input.BufferClear();
            input.BufferAppend("0x");
            input.Advance();

            while (input.CanContinue)
            {
                if (input.Current == 'p' || input.Current == 'P')
                {
                    BufferExponent();
                    break;
                }
                if (input.Current.IsHex())
                    input.BufferAppend(input.Current);
                else
                    break;
                
                input.Advance();
            }

            return input.OutputBuffer(Symbol.Number);
        }

        /* Long string literal, such as [[bla bla]] */
        Token LongStringLiteral()
        {
            input.StorePosition();
            input.BufferClear();

            int numEqualsStart = CountEquals();
            if (input.Current != '[')
                throw new CompileException(input, ExceptionMessage.INVALID_LONG_STRING_DELIMTER, input.Current);

            // Skip immediately following newline
            if (input.Current == '\r' || input.Current == '\n')
                NextLine();

            while (true)
            {
                while (input.Current != ']')
                    input.Advance();

                input.Advance();
                int numEqualsEnd = CountEquals();

                // Output string if matching ='s found

                if (numEqualsStart == numEqualsEnd && input.Current == ']')
                {
                    // Trim long string delimters
                    input.BufferRemove(0, numEqualsStart);
                    input.BufferRemove(numEqualsEnd + 1);
                    input.Advance();
                    return input.OutputBuffer(Symbol.String);
                }
                if (input.Current == ']')
                    // Parse ']' again because it can be the start of another long string delimeter
                    input.Back();
                else
                    input.BufferAppend(input.Current);
            }
        }

        /* Count amount of continous '=' */
        int CountEquals()
        {
            int count = 0;
            while (input.Current == '=')
            {
                count++;
                input.Advance();
            }

            return count;
        }

        /* Long comment, such as --[[bla bla bla]] */
        void LongComment()
        {
            input.Advance();
            int numEqualsStart = CountEquals();
            if (input.Current != '[')
                throw new CompileException(input, ExceptionMessage.INVALID_LONG_STRING_DELIMTER, input.Current);

            while (true)
            {
                while (input.Current != ']')
                    input.Advance();

                input.Advance();
                int numEqualsEnd = CountEquals();

                if (numEqualsStart == numEqualsEnd && input.Current == ']')
                {
                    input.Advance();
                    break;
                }
                // Parse ']' again because it can be the start of another long comment delimeter
                if (input.Current == ']')
                    input.Back();

            }
        }

        /* Short comment, such as --bla bla */
        void ShortComment()
        {
            while (input.CanContinue && input.Current != '\r' && input.Current != '\n')
                input.Advance();
        }

        Token Punctuation()
        {
            char c = input.Current;
            input.Advance();
            switch (c)
            {
                case '+':
                    return input.Output(Symbol.Plus);
                case '-':
                    return input.Output(Symbol.Minus);
                case '*':
                    return input.Output(Symbol.Star);
                case '/':
                    return input.Output(Symbol.Slash);
                case '%':
                    return input.Output(Symbol.Percent);
                case '^':
                    return input.Output(Symbol.Caret);
                case '#':
                    return input.Output(Symbol.Hash);
                case '~':
                    return input.Current == '=' ? LongPunctuation(c) : input.Output(Symbol.TildeEqual);
                case '<':
                    return input.Current == '=' ? LongPunctuation(c) : input.Output(Symbol.Less);
                case '>':
                    return input.Current == '=' ? LongPunctuation(c) : input.Output(Symbol.Greater);
                case '=':
                    return input.Current == '=' ? LongPunctuation(c) : input.Output(Symbol.Equal);
                case '(':
                    return input.Output(Symbol.LeftParen);
                case ')':
                    return input.Output(Symbol.RightParen);
                case '{':
                    return input.Output(Symbol.LeftBrace);
                case '}':
                    return input.Output(Symbol.RightBrace);
                case '[':
                    return input.Output(Symbol.LeftBrack);
                case ']':
                    return input.Output(Symbol.RightBrack);
                case ';':
                    return input.Output(Symbol.SemiColon);
                case ':':
                    return input.Output(Symbol.Colon);
                case ',':
                    return input.Output(Symbol.Comma);
                case '.':
                    return input.Current == '.' ? LongPunctuation(c) : input.Output(Symbol.Dot);
                default:
                    throw new CompileException(input, ExceptionMessage.UNKNOWN_PUNCTUATION, c);
            }
        }

        Token LongPunctuation(char c1)
        {
            char c2 = input.Current;
            input.Advance();

            switch(c1)
            {
                case '~':
                    if (c2 == '=') return input.Output(Symbol.TildeEqual);
                    break;
                case '<':
                    if (c2 == '=') return input.Output(Symbol.LessEqual);
                    break;
                case '>':
                    if (c2 == '=') return input.Output(Symbol.GreaterEqual);
                    break;
                case '=':
                    if (c2 == '=') return input.Output(Symbol.EqualEqual);
                    break;
                case '.':
                    if (c2 == '.')
                    {
                        if (input.Current == '.')
                        {
                            input.Advance();
                            return input.Output(Symbol.DotDotDot);
                        }
                        return input.Output(Symbol.DotDot);
                    }
                    break;
            }
            
            throw new CompileException(input, ExceptionMessage.UNKNOWN_PUNCTUATION, "" + c1 + c2);
        }

        /* String literal, such as "bla bla" */
        Token StringLiteral(char end)
        {
            input.StorePosition();
            input.BufferClear();

            while (true)
            {
                input.Advance();

                switch (input.Current)
                {
                    case '\\':
                        input.Advance();

                        switch (input.Current)
                        {
                            case 'a':  input.BufferAppend('\a'); break;
                            case 'b':  input.BufferAppend('\b'); break;
                            case 'f':  input.BufferAppend('\f'); break;
                            case 'n':  input.BufferAppend('\n'); break;
                            case 'r':  input.BufferAppend('\r'); break;
                            case 't':  input.BufferAppend('\t'); break;
                            case '\"': input.BufferAppend('\"'); break;
                            case '\'': input.BufferAppend('\''); break;
                            case '\\': input.BufferAppend('\\'); break;
                            case '\r': input.BufferAppend('\r'); NextLine(); break;
                            case '\n': input.BufferAppend('\n'); NextLine(); break;
                            default:
                                if (input.Current.IsDecimal())
                                    BufferNumericEscape();
                                else
                                    // Lua manual says only the above chars can be escaped
                                    // but Luac allows any char to be escaped
                                    input.BufferAppend(input.Current);
                                break;
                        }
                        break;

                    case '\r': case '\n':
                        throw new CompileException(input, ExceptionMessage.UNEXPECTED_EOS);

                    default:
                        if (input.Current == end)
                        {
                            input.Advance();
                            return input.OutputBuffer(Symbol.String);
                        }

                        input.BufferAppend(input.Current);
                        break;
                }
            }
        }

        /* Buffer a numeric escape, such as \012 or \9 */
        void BufferNumericEscape()
        {
            int value = 0;

            for (int i = 0; i < 3; i++)
            {
                if (!input.Current.IsDecimal())
                    break;

                value = value*10 + input.Current - '0';
                input.Advance();
            }

            input.BufferAppend((char)value);
        }

        /* New line, such as \r, \n or \r\n */
        void NextLine()
        {
            if (input.Current == '\r' && input.CanPeek && input.Peek == '\n')
                input.Advance();
            input.Advance();
            input.Newline();
        }
    }
}
