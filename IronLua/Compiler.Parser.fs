namespace IronLua.Compiler

module Parser =
    type S = Lexer.Symbol

    type State =
        val mutable Lexeme : Lexer.Lexeme
        val mutable Lexer : unit -> Lexer.Lexeme

        new(lexer) = {
            Lexeme = Unchecked.defaultof<Lexer.Lexeme>
            Lexer = lexer
        }
    
    let inline token (s:State) =
        let (symbol, _, _, _) = s.Lexeme
        symbol

    let consume (s:State) =
        s.Lexeme <- s.Lexer()

    let inline tryConsume (s:State) symbol =
        let token = token s
        if token = symbol then consume s


    let explist s =
        failwith ""

    let statement s =
        failwith ""

    let lastStatement s =
        match token s with
        | S.Return -> Some (explist s)
        | S.Break -> Some Ast.Break
        | _ -> None

    (* Parses a block
       {stat [';']} [laststat [';']] *)
    let block s =
        let rec block statements =
            if token s = S.EOF then
                statements, None
            else
                match statement s with
                | Some stat -> 
                    tryConsume s S.SemiColon
                    block (stat :: statements)
                | None ->
                    tryConsume s S.SemiColon
                    let lastStat = lastStatement s
                    tryConsume s S.SemiColon
                    statements, lastStat

        block []
                

    let parse source =
        let lexer = Lexer.create source
        let s = State(lexer)
        block s |> Ast.Block
