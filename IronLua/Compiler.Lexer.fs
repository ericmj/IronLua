namespace IronLua.Compiler

open IronLua.Error

module Lexer =

    type Lexeme = int * string * int * int

    module Char =
        let isDecimal c =
            c >= '0' && c <= '9'

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
            val mutable File : string
            val mutable Source : string
            val mutable Index : int
            val mutable Char : char
            val mutable Line : int
            val mutable Column : int
            val mutable StoredLine : int
            val mutable StoredColumn : int
            val mutable Buffer : System.Text.StringBuilder
        
            new(source) = {
                File = "<unknown>"
                Source = source
                Index = 0
                Char = '\000'
                Line = 1
                Column = 0
                StoredLine = -1
                StoredColumn = -1
                Buffer = System.Text.StringBuilder(1024)
            }

        let inline private tryIndex (s:State) f =
            try f()
            with | :? System.IndexOutOfRangeException ->  
                raise <| CompileError(s.File, (s.Line, s.Column), Message.unexpectedEOF)

        let create source =
            State(source)

        let current (s:State) =
            (fun () -> s.Source.[s.Index]) |> tryIndex s

        let canContinue (s:State) =
            s.Index < s.Source.Length

        let advance (s:State) =
            s.Index <- s.Index + 1
            s.Column <- s.Column + 1
            
        let back (s:State) =
            s.Index <- s.Index - 1
            s.Column <- s.Column - 1

        let storePosition (s:State) =
            s.StoredColumn <- s.Column
            s.StoredLine <- s.Line

        let newline (s:State) =
            s.Line <- s.Line + 1
            s.Column <- 0

        let peek (s:State) =
            (fun () -> s.Source.[s.Index+1]) |> tryIndex s

        let canPeek (s:State) =
            s.Index+1 < s.Source.Length

        let bufferAppend (s:State) (c:char) =
            s.Buffer.Append(c) |> ignore

        let bufferRemoveStart (s:State) length =
            s.Buffer.Remove(0, length) |> ignore

        let bufferRemoveEnd (s:State) length =
            s.Buffer.Remove(s.Buffer.Length-length, length) |> ignore

        let bufferClear (s:State) =
            s.Buffer.Clear() |> ignore

        let output (s:State) sym : Lexeme =
            sym, null, s.StoredLine, s.StoredColumn

        let outputBuffer (s:State) sym : Lexeme =
            sym, s.Buffer.ToString(), s.StoredLine, s.StoredColumn

        let outputEOL (s:State) : Lexeme =
            Symbol.EOL, null, s.Line, s.Column

    open Input
    open Char

    let inline faillexer (s:State) msg = raise <| CompileError(s.File, (s.Line, s.Column), msg)

    let nextLine s =
        if current s = '\r' && canPeek s && peek s = '\n' then
            advance s
        advance s
        newline s

    let bufferNumericEscape s =
        let rec bufferNumericEscape value n =
            if n >= 3 || (current s |> isDecimal |> not) then
                value
            else
                let newValue = 10*value + int (current s) - int '0'
                advance s
                bufferNumericEscape newValue (n+1)
        bufferNumericEscape 0 0 |> char |> bufferAppend s

    let stringLiteralLong s =
        storePosition s
        bufferClear s
        advance s

        let rec countEquals endChar n =
            match current s with
            | '='                -> current s |> bufferAppend s
                                    advance s
                                    countEquals endChar (n+1)
            | c when c = endChar -> n
            | c                  -> current s |> bufferAppend s
                                    advance s
                                    -1

        let numEqualsStart = countEquals '[' 0
        if numEqualsStart = -1 then
            faillexer s (Message.invalidLongStringDelimter (current s))

        // Skip immediately following newline
        match peek s with
        | '\r' | '\n' -> nextLine s
        | _           -> ()

        let rec stringLiteralLong () =
            advance s
            current s |> bufferAppend s
            match current s with
            | ']' ->
                advance s
                let numEqualsEnd = countEquals ']' 0
                if numEqualsStart = numEqualsEnd then
                    numEqualsStart |> bufferRemoveStart s
                    numEqualsEnd + 1 |> bufferRemoveEnd s
                    advance s
                    outputBuffer s Symbol.String
                else
                    back s
                    stringLiteralLong()
            | _ ->
                stringLiteralLong()

        stringLiteralLong()

    let stringLiteral s endChar =
        storePosition s
        bufferClear s

        let rec stringLiteral () =
            advance s
            match current s with            
            | '\\' ->
                advance s
                match current s with
                | 'a'  -> '\a' |> bufferAppend s
                | 'b'  -> '\b' |> bufferAppend s
                | 'f'  -> '\f' |> bufferAppend s
                | 'n'  -> '\n' |> bufferAppend s
                | 'r'  -> '\r' |> bufferAppend s
                | 't'  -> '\t' |> bufferAppend s
                | 'v'  -> '\v' |> bufferAppend s
                | '\"' -> '\"' |> bufferAppend s
                | '\'' -> '\'' |> bufferAppend s
                | '\\' -> '\\' |> bufferAppend s
                | '\r' -> '\r' |> bufferAppend s; nextLine s
                | '\n' -> '\n' |> bufferAppend s; nextLine s
                | c when isDecimal c -> bufferNumericEscape s
                | c                  -> faillexer s (Message.unknownEscapeChar c)
                stringLiteral()

            | '\r' | '\n' ->
                faillexer s Message.unexpectedEOS
                
            | c when c = endChar ->
                advance s
                advance s
                outputBuffer s Symbol.String

            | c ->
                bufferAppend s c
                stringLiteral()
        
        stringLiteral()

    let create source =
        let s = Input.create source

        let rec lexer () : Lexeme =
            if not (canContinue s) then
                outputEOL s
            else
                match current s with
                | ' ' | '\t' ->
                    advance s
                    lexer()

                | '\r' | '\n' ->
                    nextLine s
                    lexer()

                | '\'' | '"' ->
                    stringLiteral s (current s)
                | '[' ->
                    stringLiteralLong s

                | c ->
                    faillexer s (Message.unexpectedChar c)

        lexer
