namespace IronLua.Compiler

open IronLua.Error
open IronLua.Utils

module Parser =
    type S = Lexer.Symbol

    type State =
        val File : string
        val Lexer : unit -> Lexer.Lexeme
        val mutable Lexeme : Lexer.Lexeme
        val mutable NextLexeme : Lexer.Lexeme option

        new(lexer) = {
            File = "<unknown>"
            Lexer = lexer
            Lexeme = Unchecked.defaultof<Lexer.Lexeme>
            NextLexeme = None
        }

    let inline failparser (s:State) msg =
        let (_, _, line, column) = s.Lexeme
        raise <| CompileError(s.File, (line, column), msg)
    
    let inline token (s:State) =
        let (symbol, _, _, _) = s.Lexeme
        symbol

    let inline tokenData (s:State) =
        let (_, data, _, _) = s.Lexeme
        data

    let inline peekToken (s:State) =
        let (symbol, _, _, _) =
            match s.NextLexeme with
            | Some lexeme ->
                lexeme
            | None ->
                s.NextLexeme <- Some <| s.Lexer()
                s.NextLexeme.Value
        symbol

    let inline consume (s:State) =
        match s.NextLexeme with
        | Some lexeme ->
            s.Lexeme <- lexeme
            s.NextLexeme <- None
        | None ->
            s.Lexeme <- s.Lexer()

    let inline tryConsume (s:State) symbol =
        let tok = token s
        if tok = symbol then consume s

    // TODO: Internal for now, should be wrapped into a submodule with other internal/private functions
    let inline internal expect (s:State) symbol =
        let tok = token s
        consume s
        if tok <> symbol then
            failparser s <|
            Message.expectedSymbol (Lexer.prettySymbol tok) (Lexer.prettySymbol symbol)

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

    let exprlist s =
        failwith ""

    let expr s =
        failwith ""

    let args s =
        match token s with
        | S.LeftParen -> failwith ""
        | S.LeftBrace -> failwith ""
        | S.String -> let stringData = tokenData s
                      consume s
                      Ast.ArgString stringData
        | _ -> failwith "UNEXCPECTED"

    let name s =
        let ident = tokenData s
        consume s
        ident

    let varlist s vars =
        failwith ""

    let liftVarExpr prefixexpr =
        match prefixexpr with
        | Ast.VarExpr var -> var
        | _ -> failwith "UNEXCPECTED"

    let liftFuncCall prefixexpr =
        match prefixexpr with
        | Ast.FuncCall funccall -> funccall
        | _ -> failwith "UNEXCPECTED"

    let rec prefixExpr s prefixexpr =
        match token s with
        | S.LeftBrack ->
            consume s
            let entry = Ast.TableEntry (prefixexpr, expr s) |> Ast.VarExpr |> prefixExpr s
            expect s S.RightBrack
            entry
        | S.Dot ->
            consume s
            Ast.TableDot (prefixexpr, name s) |> Ast.VarExpr |> prefixExpr s
        | S.Colon ->
            consume s
            Ast.FuncCallObject (prefixexpr, name s, args s) |> Ast.FuncCall |> prefixExpr s
        | S.LeftParen | S.LeftBrack | S.String ->
            Ast.FuncCallNormal (prefixexpr, args s) |> Ast.FuncCall |> prefixExpr s
        | _ ->
            prefixexpr

    let assignOrFunccall s =
        let preexpr =
            match token s with
            | S.Identifier ->
                prefixExpr s (Ast.VarExpr <| Ast.Name (name s))
            | S.LeftParen ->
                consume s
                let pexpr = prefixExpr s (expr s)
                expect s S.RightParen
                pexpr
            | _ ->
                failwith "FAIL THIS SHIET"

        match token s with
        | S.Comma ->
            varlist s [liftVarExpr preexpr]
        | S.Equal ->
            consume s
            Ast.Assign ([liftVarExpr preexpr], expr s)
        | _ ->
            liftFuncCall preexpr |> Ast.StatFuncCall
            

    let statement s =
        match token s with
        | S.Do       -> Left  <| do' s
        | S.While    -> Left  <| while' s
        | S.Repeat   -> Left  <| repeat s
        | S.If       -> Left  <| if' s
        | S.For      -> Left  <| for' s
        | S.Function -> Left  <| function' s
        | S.Local    -> Left  <| local s
        | S.Return   -> Right <| exprlist s
        | S.Break    -> Right <| Ast.Break 
        | _          -> Left  <| assignOrFunccall s

    (* Parses a block
       {stat [';']} [laststat [';']] *)
    let block s =
        let rec block statements =
            if token s = S.EOF then
                (statements, None) |> Ast.Block
            else
                match statement s with
                | Left stat ->
                    tryConsume s S.SemiColon
                    block (stat :: statements)
                | Right lastStat ->
                    tryConsume s S.SemiColon
                    expect s S.EOF
                    (statements, Some lastStat) |> Ast.Block

        block []
                

    let parse source =
        let lexer = Lexer.create source
        let s = State(lexer)
        consume s
        block s
