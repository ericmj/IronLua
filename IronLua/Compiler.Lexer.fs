namespace IronLua.Compiler

module Lexer =
    type Lexeme = int * string * int * int

    module Symbol =
        // Keywords
        let [<Literal>] And = 0
        let [<Literal>] Break = 1
        let [<Literal>] Do = 2
        let [<Literal>] Else = 3
        let [<Literal>] Elseif = 4
        let [<Literal>] End = 5
        let [<Literal>] False = 6
        let [<Literal>] For = 7
        let [<Literal>] Function = 8
        let [<Literal>] If = 9
        let [<Literal>] In = 10
        let [<Literal>] Local = 11
        let [<Literal>] Nil = 12
        let [<Literal>] Not = 13
        let [<Literal>] Or = 14
        let [<Literal>] Repeat = 15
        let [<Literal>] Return = 16
        let [<Literal>] Then = 17
        let [<Literal>] True = 18
        let [<Literal>] Until = 19
        let [<Literal>] While = 20

        // Punctuations
        let [<Literal>] Plus = 100
        let [<Literal>] Minus = 101
        let [<Literal>] Star = 102
        let [<Literal>] Slash = 103
        let [<Literal>] Percent = 104
        let [<Literal>] Carrot = 105
        let [<Literal>] Hash = 106
        let [<Literal>] EqualEqual = 107
        let [<Literal>] TildeEqual = 108
        let [<Literal>] LessEqual = 109
        let [<Literal>] GreaterEqual = 110
        let [<Literal>] Less = 111
        let [<Literal>] Greater = 112
        let [<Literal>] Equal = 113
        let [<Literal>] LeftParen = 114
        let [<Literal>] RightParen = 115
        let [<Literal>] LeftBrace = 116
        let [<Literal>] RightBrace = 117
        let [<Literal>] LeftBrack = 118
        let [<Literal>] RightBrack = 119
        let [<Literal>] SemiColon = 120
        let [<Literal>] Colon = 121
        let [<Literal>] Comma = 122
        let [<Literal>] Dot = 123
        let [<Literal>] DotDot = 124
        let [<Literal>] DotDotDot = 125

        // Literals
        let [<Literal>] Number = 200
        let [<Literal>] String = 201
        let [<Literal>] Identifier = 202

        // Markers
        let [<Literal>] EOL = 300

    let keywords =
        [
        "and", Symbol.And;
        "break", Symbol.Break;
        "do", Symbol.Do;
        "else", Symbol.Else;
        "elseif", Symbol.Elseif;
        "end", Symbol.End;
        "false", Symbol.False;
        "for", Symbol.For;
        "function", Symbol.Function;
        "if", Symbol.If;
        "in", Symbol.In;
        "local", Symbol.Local;
        "nil", Symbol.Nil;
        "not", Symbol.Not;
        "or", Symbol.Or;
        "repeat", Symbol.Repeat;
        "return", Symbol.Return;
        "then", Symbol.Then;
        "true", Symbol.True;
        "until", Symbol.Until;
        "while", Symbol.While;
        ] |> dict

    let punctuations = 
        [
        "+", Symbol.Plus;
        "-", Symbol.Minus;
        "*", Symbol.Star;
        "/", Symbol.Slash;
        "%", Symbol.Percent;
        "^", Symbol.Carrot;
        "#", Symbol.Hash;
        "==", Symbol.EqualEqual;
        "~=", Symbol.TildeEqual;
        "<=", Symbol.LessEqual;
        ">=", Symbol.GreaterEqual;
        "<", Symbol.Less;
        ">", Symbol.Greater;
        "=", Symbol.Equal;
        "(", Symbol.LeftParen;
        ")", Symbol.RightParen;
        "{", Symbol.LeftBrace;
        "}", Symbol.RightBrace;
        "[", Symbol.LeftBrack;
        "]", Symbol.RightBrack;
        ";", Symbol.SemiColon;
        ":", Symbol.Colon;
        ",", Symbol.Comma;
        ".", Symbol.Dot;
        "..", Symbol.DotDot;
        "...", Symbol.DotDotDot;
        ] |> dict

    module Input =
        type State =
            val mutable Source : string
            val mutable Index : int
            val mutable Char : char
            val mutable Line : int
            val mutable Column : int
            val mutable StoredLine : int
            val mutable StoredColumn : int
            val mutable Buffer : System.Text.StringBuilder
        
            new(source) = {
                Source = source
                Index = 0
                Char = '\000'
                Line = 1
                Column = 0
                StoredLine = -1
                StoredColumn = -1
                Buffer = System.Text.StringBuilder(1024)
            }

        let create source =
            State(source)

        let current (s:State) =
            s.Source.[s.Index]

        let canContinue (s:State) =
            s.Index < s.Source.Length

        let advance (s:State) =
            s.Index <- s.Index + 1
            s.Column <- s.Column + 1

        let newline (s:State) =
            s.Line <- s.Line + 1
            s.Column <- 0

        let peek (s:State) =
            s.Source.[s.Index+1]

        let canPeek (s:State) =
            s.Index+1 < s.Source.Length

        let bufferAppend (s:State) (c:char) =
            s.Buffer.Append(c) |> ignore

        let bufferClear (s:State) =
            s.Buffer.Clear() |> ignore

        let output (s:State) sym : Lexeme =
            sym, null, s.StoredLine, s.StoredColumn

        let outputBuffer (s:State) sym : Lexeme =
            sym, s.Buffer.ToString(), s.StoredLine, s.StoredColumn

        let outputEOL (s:State) : Lexeme =
            Symbol.EOL, null, s.Line, s.Column

    open Input

    let create source =
        let s = Input.create source
        let rec lexer () =
            ()

        lexer
