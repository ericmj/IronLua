namespace IronLua.Compiler

open IronLua.Error

module Parser =
    type S = Lexer.Symbol

    type State =
        val File : string
        val Lexer : unit -> Lexer.Lexeme
        val mutable Lexeme : Lexer.Lexeme

        new(lexer) = {
            File = "<unknown>"
            Lexer = lexer
            Lexeme = Unchecked.defaultof<Lexer.Lexeme>
        }
    
    let inline token (s:State) =
        let (symbol, _, _, _) = s.Lexeme
        symbol

    let consume (s:State) =
        s.Lexeme <- s.Lexer()

    let inline tryConsume (s:State) symbol =
        let token = token s
        if token = symbol then consume s

    let inline private failparser (s:State) msg =
        let (_, _, line, column) = s.Lexeme
        raise <| CompileError(s.File, (line, column), msg)

    let explist s =
        failwith ""

    let do' s =
        failwith ""

    let while' s =
        failwith ""

    let repeat s =
        failwith ""

    let if' s =
        failwith ""

    let for' s =
        failwith ""

    let function' s =
        failwith ""

    let local s =
        failwith ""

    let prefixexpr s =
        failwith ""

    let exprlist s =
        failwith ""

    let varlistOrFunccall s =
        let prefix = prefixexpr s
        match token s with
        // FuncCall
        | S.Colon     -> failwith ""
        | S.LeftParen -> failwith ""
        | S.LeftBrace -> failwith ""
        | S.String    -> failwith ""
                         
        // Assignment    
        | S.LeftBrack -> failwith ""
        | S.Dot       -> failwith ""
        // TODO: Make sure prefixexp only parsed into Name and transform into Name
        | S.Comma     -> failwith ""
        | S.Equal     -> failwith ""
        
        | sym         -> Lexer.prettySymbol sym |> Message.unexpectedSymbol |> failparser s

    let statement s =
        match token s with
        | S.Do       -> do' s
        | S.While    -> while' s
        | S.Repeat   -> repeat s
        | S.If       -> if' s
        | S.For      -> for' s
        | S.Function -> function' s
        | S.Local    -> local s
        | _          -> varlistOrFunccall s

    let lastStatement s =
        match token s with
        | S.Return -> Some (exprlist s)
        | S.Break  -> Some Ast.Break
        | _        -> None

    (* Parses a block
       {stat [';']} [laststat [';']] *)
    let block s =
        let rec block statements =
            if token s = S.EOF then
                (statements, None) |> Ast.Block
            else
                match statement s with
                | Some stat -> 
                    tryConsume s S.SemiColon
                    block (stat :: statements)
                | None ->
                    tryConsume s S.SemiColon
                    let lastStat = lastStatement s
                    tryConsume s S.SemiColon
                    (statements, lastStat) |> Ast.Block

        block []
                

    let parse source =
        let lexer = Lexer.create source
        let s = State(lexer)
        block s
