namespace IronLua

open IronLua.Compiler

module Program =
    let lexer = Lexer.create ""
    let dprintfn str = System.Diagnostics.Debug.WriteLine(str)
    let lexprint () = lexer() |> string |> dprintfn

    lexprint()
