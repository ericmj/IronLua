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

        // Markers
        let [<Literal>] EOL = 100

        let keywords =
          [
            "and", And;
            "break", Break;
            "do", Do;
            "else", Else;
            "elseif", Elseif;
            "end", End;
            "false", False;
            "for", For;
            "function", Function;
            "if", If;
            "in", In;
            "local", Local;
            "nil", Nil;
            "not", Not;
            "or", Or;
            "repeat", Repeat;
            "return", Return;
            "then", Then;
            "true", True;
            "until", Until;
            "while", While;
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
