using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IronLua_CSharp.Compiler
{
    class Lexer
    {
        static readonly Dictionary<string, Symbol> keywords =
            new Dictionary<string, Symbol>()
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
            new Dictionary<string,Symbol>()
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

        public Lexer(Input input)
        {
            this.input = input;
        }

        public Token Next()
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
                        if (input.Current.IsFirstPunctuation())
                            return Punctuation();

                        throw new CompileException(input.File, input.Line, input.Column,
                                                   String.Format(ExceptionMessage.UNEXPECTED_CHAR, input.Current));

                }
            }
            throw new NotImplementedException();
        }

        Token IdentifierOrKeyword()
        {
            throw new NotImplementedException();
        }

        Token NumericLiteral()
        {
            throw new NotImplementedException();
        }

        Token NumericHexLiteral()
        {
            throw new NotImplementedException();
        }

        Token LongStringLiteral()
        {
            throw new NotImplementedException();
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
                // Parse ']' again because it can be the start of another long string delimeter
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
