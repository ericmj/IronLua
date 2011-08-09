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
                    case ' ': case '\t':
                        input.Advance();
                        break;
                    case '\r': case '\n':
                        NextLine();
                        break;
                }
            }
            throw new NotImplementedException();
        }

        private void NextLine()
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
