using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IronLua_CSharp.Compiler
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
                    {"^",   Symbol.Carrot},
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
                throw new CompileException(input.File, input.Line, input.Column,
                                           String.Format(ExceptionMessage.UNEXPECTED_SYMBOL, symbol));
        }

        private Token NextToken()
        {
            while (!input.CanContinue)
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

                        throw new CompileException(input.File, input.Line, input.Column,
                                                   String.Format(ExceptionMessage.UNEXPECTED_CHAR, input.Current));

                }
            }
            return input.Output(Symbol.Eof);
        }

        // Identifier or keyword
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

        // Numeric literal, such as 12345 or 45e+1
        Token NumericLiteral()
        {
            input.StorePosition();
            input.BufferClear();
            input.BufferAppend(input.Current);

            while (input.CanContinue)
            {
                input.Advance();

                if (input.Current == 'e' || input.Current == 'E')
                {
                    BufferExponent();
                    break;
                }
                if (input.Current.IsDecimal())
                    input.BufferAppend(input.Current);
                else
                    break;
            }

            return input.OutputBuffer(Symbol.Number);
        }

        // Buffers the exponent part of a numeric literal,
        // such as e+5 p8 e2
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

        // Hex literal, such as 0xFF or 0x10p4
        // Can be malformed, parser handles that
        Token NumericHexLiteral()
        {
            input.StorePosition();
            input.BufferClear();
            input.BufferAppend("0x");
            input.Advance();

            while (input.CanContinue)
            {
                input.Advance();

                if (input.Current == 'p' || input.Current == 'P')
                {
                    BufferExponent();
                    break;
                }
                if (input.Current.IsHex())
                    input.BufferAppend(input.Current);
                else
                    break;
            }

            return input.OutputBuffer(Symbol.Number);
        }

        // Long string literal, such as [[bla bla]]
        Token LongStringLiteral()
        {
            input.StorePosition();
            input.BufferClear();

            int numEqualsStart = CountEquals();
            if (input.Current != '[')
                throw new CompileException(input.File, input.Line, input.Column,
                                           String.Format(ExceptionMessage.INVALID_LONG_STRING_DELIMTER, input.Current));

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

        // Long comment, such as --[[bla bla bla]]
        void LongComment()
        {
            input.Advance();
            int numEqualsStart = CountEquals();
            if (input.Current != '[')
                throw new CompileException(input.File, input.Line, input.Column,
                                           String.Format(ExceptionMessage.INVALID_LONG_STRING_DELIMTER, input.Current));

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

        // Short comment, such as --bla bla
        void ShortComment()
        {
            while (input.CanContinue && input.Current != '\r' && input.Current != '\n')
                input.Advance();
        }

        Token Punctuation()
        {
            var punctuationBuilder = new StringBuilder(3);
            while (input.CanContinue && input.Current.IsPunctuation())
                punctuationBuilder.Append(input.Current);

            var punctuation = punctuationBuilder.ToString();
            Symbol symbol;
            if (punctuations.TryGetValue(punctuation, out symbol))
                return input.Output(symbol);

            throw new CompileException(input.File, input.Line, input.Column,
                                       String.Format(ExceptionMessage.UNKNOWN_PUNCTUATION, punctuation));
        }

        // String literal, such as "bla bla"
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
                        throw new CompileException(input.File, input.Line, input.Column,
                                                   ExceptionMessage.UNEXPECTED_EOS);

                    default:
                        if (input.Current == end)
                        {
                            input.Skip(2);
                            return input.OutputBuffer(Symbol.String);
                        }

                        input.BufferAppend(input.Current);
                        break;
                }
            }
        }

        // Buffer a numeric escape, such as \012 or \9
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

        void NextLine()
        {
            if (input.Current == '\r' && input.CanPeek && input.Peek == '\n')
                input.Advance();
            input.Advance();
            input.Newline();
        }

        public class Token
        {
            public Symbol Symbol { get; private set; }
            public int Line { get; private set; }
            public int Column { get; private set; }
            public string Lexeme { get; private set; }

            public Token(Symbol symbol, int line, int column, string lexeme = null)
            {
                Symbol = symbol;
                Lexeme = lexeme;
                Line = line;
                Column = column;
            }
        }
    }
}
