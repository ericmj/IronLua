namespace IronLua

open IronLua.Compiler

module Program =
    let lexer = Lexer.create ""
    let dprintfn str = System.Diagnostics.Debug.WriteLine(str)

    let rec lexify () =
        match lexer() with
        | (Lexer.Symbol.EOF, _, _, _) -> ()
        | lexeme -> lexeme |> string |> dprintfn; lexify()

    lexify()