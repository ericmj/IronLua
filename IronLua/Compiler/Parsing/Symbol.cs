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
        Shebang,         // '#!' at start of file
        Error,
        Eol,
        Eof,

        // Aliases
        EndOfLine = Eol,
        EndOfFile = Eof,
        EndOfStream = Eof,
    }
}