using System.Collections.Generic;
using Microsoft.Scripting.Utils;

namespace IronLua.Compiler.Parsing
{
    public enum Symbol
    {
        // Keywords
        And,
        Break,
        Do,
        Else,
        Elseif,
        End,
        False,
        For,
        Function,
        Goto,    // Lua 5.2 feature
        If,
        In,
        Local,
        Nil,
        Not,
        Or,
        Repeat,
        Return,
        Then,
        True,
        Until,
        While,

        // Punctuations
        Plus,
        Minus,
        Star,
        Slash,
        Percent,
        Caret,
        Hash,
        Tilde,
        EqualEqual,
        TildeEqual,
        LessEqual,
        GreaterEqual,
        Less,
        Greater,
        Equal,
        LeftParen,
        RightParen,
        LeftBrace,
        RightBrace,
        LeftBrack,
        RightBrack,
        SemiColon,
        Colon,
        ColonColon,
        Comma,
        Dot,
        DotDot,
        DotDotDot,

        // Literals
        Number,
        String,
        Identifier,

        // Markers
        Comment,
        Whitespace,
        Error,
        Eol,
        Eof,

        // Aliases
        EndOfLine = Eol,
        EndOfFile = Eof,
        EndOfStream = Eof,
    }

    public static class SymbolExtensions
    {
        public static readonly IDictionary<Symbol, string> TokenStrings =
            new Dictionary<Symbol, string>()
            {
                // Keywords
                {Symbol.And,          "and"},
                {Symbol.Break,        "break"},
                {Symbol.Do,           "do"},
                {Symbol.Else,         "else"},
                {Symbol.Elseif,       "elseif"},
                {Symbol.End,          "end"},
                {Symbol.False,        "false"},
                {Symbol.For,          "for"},
                {Symbol.Function,     "function"},
                {Symbol.Goto,         "goto"},
                {Symbol.If,           "if"},
                {Symbol.In,           "in"},
                {Symbol.Local,        "local"},
                {Symbol.Nil,          "nil"},
                {Symbol.Not,          "not"},
                {Symbol.Or,           "or"},
                {Symbol.Repeat,       "repeat"},
                {Symbol.Return,       "return"},
                {Symbol.Then,         "then"},
                {Symbol.True,         "true"},
                {Symbol.Until,        "until"},
                {Symbol.While,        "while"},
                // Punctuations
                {Symbol.Plus,         "+"},
                {Symbol.Minus,        "-"},
                {Symbol.Star,         "*"},
                {Symbol.Slash,        "/"},
                {Symbol.Percent,      "%"},
                {Symbol.Caret,        "^"},
                {Symbol.Hash,         "#"},
                {Symbol.Equal,        "="},
                {Symbol.EqualEqual,   "=="},
                {Symbol.Tilde,        "~"},
                {Symbol.TildeEqual,   "~="},
                {Symbol.Less,         "<"},
                {Symbol.LessEqual,    "<="},
                {Symbol.Greater,      ">"},
                {Symbol.GreaterEqual, ">="},
                {Symbol.SemiColon,    ";"},
                {Symbol.Colon,        ":"},
                {Symbol.ColonColon,   "::"},
                {Symbol.Comma,        ","},
                {Symbol.Dot,          "."},
                {Symbol.DotDot,       ".."},
                {Symbol.DotDotDot,    "..."},
                // Punctuations (matching)
                {Symbol.LeftParen,    "("},
                {Symbol.RightParen,   ")"},
                {Symbol.LeftBrace,    "{"},
                {Symbol.RightBrace,   "}"},
                {Symbol.LeftBrack,    "["},
                {Symbol.RightBrack,   "]"},
                // Literals
                {Symbol.Number,       "<number>"},
                {Symbol.Identifier,   "<name>"},
                {Symbol.String,       "<string>"},
                // Markers
                {Symbol.Eof,          "<eof>"},
                {Symbol.Eol,          "<eol>"},
                {Symbol.Error,        "<error>"},
                {Symbol.Whitespace,   "<space>"},
                {Symbol.Comment,      "<comment>"},
            };

        public static string ToTokenString(this Symbol symbol)
        {
            string tokenString;
            if (!TokenStrings.TryGetValue(symbol, out tokenString))
            {
                throw Assert.Unreachable; // should never happen
            }
            return tokenString;
        }
    }
}