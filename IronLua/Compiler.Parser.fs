namespace IronLua.Compiler

module Parser =
    type State =
        val mutable Lexeme : Lexer.Lexeme
        val mutable Lexer : unit -> Lexer.Lexeme

        new(lexer) = {
            Lexeme = Unchecked.defaultof<Lexer.Lexeme>
            Lexer = lexer
        }

    let block s =
        failwith ""

    let parse source =
        let lexer = Lexer.create source
        let s = State(lexer)
        block s |> Ast.Block