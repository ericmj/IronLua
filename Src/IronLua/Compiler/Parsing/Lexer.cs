using System.Collections.Generic;
using System.Diagnostics;
using IronLua.Util;
using Microsoft.Scripting;

namespace IronLua.Compiler.Parsing
{
    class Lexer : ILexer
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

        readonly Input input;

        public Lexer(Input input)
        {
            this.input = input;
        }

        #region ILexer members

        public LuaSyntaxException SyntaxException(string message)
        {
            return input.SyntaxException(message);
        }
        
        public LuaSyntaxException SyntaxException(string format, params object[] args)
        {            
            return SyntaxException(System.String.Format(format, args));
        }

        public SourceUnit SourceUnit
        {
            get { return null; } // TODO: this is where our code comes from.
        }

        public Token GetNextToken()
        {
            Token token = NextToken();

            return token;
        }

        #endregion


        private Token NextToken()
        {
            while (input.CanContinue)
            {
                switch (input.Current)
                {
                    // end of stream
                    case (char)0xFFFF:
                        return input.Output(Symbol.Eof);

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
                           (input.Peek == '[' || input.Peek == '='))
                            return LongStringLiteral();

                        // Hex numeric
                        if (input.Current == '0' && input.CanPeek &&
                           (input.Peek == 'X' || input.Peek == 'x'))
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

                        throw SyntaxException(ExceptionMessage.UNEXPECTED_CHAR, input.Current);

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
            input.Advance(); // first [

            int numEqualsStart = CountEquals();
            if (input.Current != '[')
                throw SyntaxException(ExceptionMessage.INVALID_LONG_STRING_DELIMTER, input.Current);
            input.Advance(); // second [

            // Skip immediately following newline
            if (input.Current == '\r' || input.Current == '\n')
                NextLine();

            while (true)
            {
                while (input.Current != ']')
                {
                    if (input.Current == '\r' && input.CanPeek && input.Peek == '\n')
                        input.Advance(); // convert CRLF to LF
                    input.BufferAppend(input.Current);
                    input.Advance();
                }
                
                input.Advance(); // first ]
                int numEqualsEnd = CountEquals();

                // Output string if matching ='s found

                if (input.Current == ']' && numEqualsStart == numEqualsEnd)
                {
                    input.Advance(); // second ]
                    return input.OutputBuffer(Symbol.String);
                }

                // Add the ] and = characters to buffer
                input.BufferAppend(']'); // first ]
                for (int i = 0; i < numEqualsEnd; ++i)
                {
                    input.BufferAppend('=');
                }
                if (input.Current == ']')
                {
                    input.BufferAppend(']'); // second ]
                    input.Advance();
                }
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
            input.Advance(); // minus

            input.Advance(); // first [
            int numEqualsStart = CountEquals();
            if (input.Current != '[')
                throw SyntaxException(ExceptionMessage.INVALID_LONG_STRING_DELIMTER, input.Current);
            input.Advance(); // second [

            while (true)
            {
                while (input.Current != ']')
                    input.Advance();

                input.Advance(); // first ]
                int numEqualsEnd = CountEquals();

                if (input.Current == ']' && numEqualsStart == numEqualsEnd)
                {
                    input.Advance(); // second ]
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
                    throw SyntaxException(ExceptionMessage.UNKNOWN_PUNCTUATION, c);
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

            throw SyntaxException(ExceptionMessage.UNKNOWN_PUNCTUATION, "" + c1 + c2);
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
                            case 'x':  
                                //if (false) // (options.LuaVersion < "Lua 5.2") // TODO: compiler options
                                //    goto default;
                                BufferHexEscape(); 
                                break;
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
                        throw SyntaxException(ExceptionMessage.UNEXPECTED_EOS);

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
            Debug.Assert(input.Current.IsDecimal());

            int value = input.Current - '0';

            for (int i = 1; i < 3; ++i)
            {
                if (!(input.CanPeek && input.Peek.IsDecimal()))
                    break;

                input.Advance();
                value = value*10 + input.Current - '0';
            }

            input.BufferAppend((char)value);
        }

        /* Buffer a hex excape, sutch as \x3F or \xa8 */
        void BufferHexEscape() // Lua 5.2 feature
        {
            Debug.Assert(input.Current == 'x');

            string strValue = "\\x";
            int value = 0;

            for (int i = 0; i < 2; ++i)
            {
                input.Advance();
                strValue += input.Current;

                char c = input.Current;
                if ('0' <= c && c <= '9')
                    value = (value << 4) | (input.Current - '0');
                else if ('a' <= c && c <= 'f')
                    value = (value << 4) | (input.Current - 'a' + 10);
                else if ('A' <= c && c <= 'F')
                    value = (value << 4) | (input.Current - 'A' + 10);
                else
                    throw SyntaxException("hexadecimal digit expected near '{0}'", strValue);
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
