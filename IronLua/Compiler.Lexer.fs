namespace IronLua.Compiler

module Lexer =
    module Symbol =
        let [<Literal>] EOL = 0

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

    let output (s:State) sym =
        sym, null, s.StoredLine, s.StoredColumn

    let outputBuffer (s:State) sym =
        sym, s.Buffer.ToString(), s.StoredLine, s.StoredColumn

    let outputEOL (s:State) =
        Symbol.EOL, null, s.Line, s.Column
