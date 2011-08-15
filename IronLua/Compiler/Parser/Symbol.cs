namespace IronLua.Compiler.Parser
{
    enum Symbol
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
        Carrot,
        Hash,
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
        Comma,
        Dot,
        DotDot,
        DotDotDot,

        // Literals
        Number,
        String,
        Identifier,

        // Markers
        Eof
    }
}