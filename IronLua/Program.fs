namespace IronLua

open IronLua.Compiler

module Program =
    let dprintfn str = System.Diagnostics.Debug.WriteLine(str)

    let testLexer () =
        let lexer = Lexer.create ""

        let rec lexify () =
            match lexer() with
            | (Lexer.Symbol.EOF, _, _, _) -> ()
            | lexeme -> lexeme |> string |> dprintfn; lexify()

        lexify()

    let testParser () =
        let ast = Parser.parse ""
        dprintfn "DONE!"

    testParser()