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
                        return CommentOrMinus();

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

        Token CommentOrMinus()
        {
            //storePosition s
            //advance s

            //match current s with
            //| '-' ->
            //    if canPeek s && peek s = '['
            //        then longComment s
            //        else shortComment s
            //    lexer()
            //| _   ->
            //    Symbol.Minus |> output s
            throw new NotImplementedException();
        }

        Token Punctuation()
        {
            // let symbol = punctuation s
            // advance s
            // output s symbol
            throw new NotImplementedException();
        }

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
