namespace IronLua.Compiler

open IronLua.Error
open IronLua.Utils

// TODO: Fix access control
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

    let inline internal failparserExpected (s:State) sym1 sym2 =
        let msg = Message.expectedSymbol (Lexer.prettySymbol sym1) (Lexer.prettySymbol sym2)
        failparser s msg

    let inline internal failparserUnexpected (s:State) sym =
        Lexer.prettySymbol sym |> Message.unexpectedSymbol |> failparser s
    
    let inline symbol (s:State) =
        let (sym, _, _, _) = s.Lexeme
        sym

    let inline value (s:State) =
        let (_, data, _, _) = s.Lexeme
        data

    let inline peekSymbol (s:State) =
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

    let inline tryConsume (s:State) sym =
        let tok = symbol s
        if tok = sym then consume s

    let inline internal expect (s:State) sym =
        let sym2 = symbol s
        consume s
        if sym2 <> sym then
            failparserExpected s sym2 sym

    let consumeValue s =
        let val' = value s
        consume s
        val'

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

    let tableconstr s =
        failwith ""

    (* Parses args
       '(' [explist] ')' | '{' [fieldlist] '}' | String *)
    let args s =
        match symbol s with
        | S.LeftParen ->
            consume s
            let exprs = exprlist s |> Ast.ArgsNormal 
            expect s S.RightParen
            exprs
        | S.LeftBrace ->
            consume s
            let table = tableconstr s |> Ast.ArgsTable
            expect s S.RightBrack
            table
        | S.String ->
            consumeValue s |> Ast.ArgString
        | sym ->
            failparserUnexpected s sym

    // Tries to lift Ast.VarExpr from Ast.PrefixExpr
    let liftVarExpr prefixexpr =
        match prefixexpr with
        | Ast.VarExpr var -> Some var
        | _               -> None

    // Tries to lift Ast.FuncCall from Ast.PrefixExpr
    let liftFuncCall prefixexpr =
        match prefixexpr with
        | Ast.FuncCall funccall -> Some funccall
        | _                     -> None

    (* Parses a prefixexpr, bottom-up parsing
       Finds the terminals Name or '(', if '(' is found parse an expr,
       if Name is found parse from bottom up into a functioncall or var
       prefixexp ::= var | functioncall | '(' exp ')'
       var ::= Name | prefixexp '[' exp ']' | prefixexp '.' Name
       functioncall ::= prefixexp args | prefixexp ':' Name args *)
    let prefixExpr s =
        let rec prefixExpr leftAst =
            match symbol s with
            // Var
            | S.LeftBrack ->
                consume s
                let entry = Ast.TableEntry (leftAst, expr s) |> Ast.VarExpr |> prefixExpr
                expect s S.RightBrack
                entry
            | S.Dot ->
                consume s
                Ast.TableDot (leftAst, consumeValue s) |> Ast.VarExpr |> prefixExpr

            // Function call
            | S.Colon ->
                consume s
                Ast.FuncCallObject (leftAst, consumeValue s, args s) |> Ast.FuncCall |> prefixExpr
            | S.LeftParen | S.LeftBrack | S.String ->
                Ast.FuncCallNormal (leftAst, args s) |> Ast.FuncCall |> prefixExpr

            // Unrecognized, return what we have
            | _ ->
                leftAst

        // Parse the terminal/bottom symbol of the prefix expression
        match symbol s with
        | S.Identifier ->
            prefixExpr (Ast.VarExpr <| Ast.Name (consumeValue s))
        | S.LeftParen ->
            consume s
            let pexpr = prefixExpr (expr s)
            expect s S.RightParen
            pexpr
        | sym ->
            failparserUnexpected s sym

    (* Parses a varlist
       var {',' var} *)
    let rec varlist s vars =
        consume s
        let preexpr = prefixExpr s

        match symbol s with
        | S.Comma ->
            match liftVarExpr preexpr with
            | Some var -> varlist s (var :: vars)
            | None     -> failparserUnexpected s S.Comma
        | _ ->
            vars

    (* Parses either an assignment or a function call 
       var {',' var} '=' expr {',' expr} |
       functioncall *)
    let assignOrFunccall s =
        let preexpr = prefixExpr s

        match symbol s with
        // We know it's an assignment so parse the rest of the vars as that
        | S.Comma ->
            let vars =
                match liftVarExpr preexpr with
                | Some var -> varlist s [var]
                | None     -> failparserUnexpected s S.Comma
            expect s S.Equal
            Ast.Assign (vars, exprlist s)

        // We know it's an assignment and a single var was parsed so parse the
        // expression list after '='
        | S.Equal ->
            consume s
            match liftVarExpr preexpr with
            | Some var -> Ast.Assign ([var], exprlist s)
            | None     -> failparserUnexpected s S.Equal

        // Not an assignment, has to be a function call
        | sym ->
            match liftFuncCall preexpr with
            | Some funcCall -> Ast.StatFuncCall funcCall
            | None          -> failparserUnexpected s sym
            
    (* Parses a statement (Left) or a last statement (Right) *)
    let statement s =
        match symbol s with
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
            if symbol s = S.EOF then
                (statements, None)
            else
                match statement s with
                // Statement
                | Left stat ->
                    tryConsume s S.SemiColon
                    block (stat :: statements)
                // Last statement
                | Right lastStat ->
                    tryConsume s S.SemiColon
                    expect s S.EOF
                    (statements, Some lastStat)

        block []
                

    let parse source : Ast.Block =
        let lexer = Lexer.create source
        let s = State(lexer)
        consume s
        block s
