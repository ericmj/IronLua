namespace IronLua.Compiler

open IronLua.Error
open IronLua.Utils

// TODO: Fix access control
module Parser =
    type S = Lexer.Symbol
    type UnaryOp = Ast.UnaryOp
    type BinaryOp = Ast.BinaryOp
    type NumStyles = System.Globalization.NumberStyles

    let cultureInfo = System.Globalization.CultureInfo("en-US")

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

    // NOTE: On second thought this shouldn't really be used
    //       value s/consume s conveys what's happening better
    let consumeValue s =
        let val' = value s
        consume s
        val'


    let isUnaryOp sym =
        match sym with
        | S.Minus | S.Not | S.Hash -> true
        | _ -> false

    let isBinaryOp sym =
        match sym with
        | S.Plus | S.Minus | S.Star | S.Slash | S.Carrot | S.Percent
        | S.DotDot | S.Less | S.LessEqual | S.Greater | S.GreaterEqual
        | S.EqualEqual | S.TildeEqual | S.And | S.Or -> true
        | _ -> false

    let unaryOpPriority =
        8
    
    let binaryOpPriority sym =
        match sym with
        | S.Or -> (1, 1)
        | S.And -> (2, 2)
        | S.Less | S.Greater | S.LessEqual | S.GreaterEqual
        | S.TildeEqual | S.Equal -> (3, 3)
        | S.DotDot -> (5, 4) // right assoc
        | S.Plus | S.Minus -> (6, 6)
        | S.Star | S.Slash | S.Percent -> (7, 7)
        | S.Carrot -> (10, 9) // right assoc
        | _ -> (-1, -1)

    // Helpers for binary op priority
    let leftBinaryPrio s =
        symbol s |> binaryOpPriority |> fst
    let rightBinaryPrio s =
        symbol s |> binaryOpPriority |> snd

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

    let unaryOp s =
        let sym = symbol s
        consume s
        match sym with
        | S.Minus -> UnaryOp.Negative
        | S.Not   -> UnaryOp.Not
        | S.Hash  -> UnaryOp.Length
        | _       -> Unchecked.defaultof<UnaryOp>

    let binaryOp s =
        let sym = symbol s
        consume s
        match sym with
        | S.Or           -> BinaryOp.Or
        | S.And          -> BinaryOp.And
        | S.Less         -> BinaryOp.Less
        | S.Greater      -> BinaryOp.Greater
        | S.LessEqual    -> BinaryOp.LessEqual
        | S.GreaterEqual -> BinaryOp.GreaterEqual
        | S.TildeEqual   -> BinaryOp.NotEqual
        | S.Equal        -> BinaryOp.Equal
        | S.DotDot       -> BinaryOp.Concat
        | S.Plus         -> BinaryOp.Add
        | S.Minus        -> BinaryOp.Subtract
        | S.Star         -> BinaryOp.Multiply
        | S.Slash        -> BinaryOp.Divide
        | S.Percent      -> BinaryOp.Mod
        | S.Carrot       -> BinaryOp.Raise
        | _              -> Unchecked.defaultof<BinaryOp>

    let hexNumber (str: string) =
        let styles = NumStyles.AllowHexSpecifier
        let expIndex = str.IndexOfAny([|'p'; 'P'|])
        let decimal, exp =
            if expIndex = -1 then
                System.UInt64.Parse(str, styles, cultureInfo), 0UL
            else
                let decimalPart, expPart = str.Substring(0, expIndex), str.Substring(expIndex+1)
                System.UInt64.Parse(decimalPart, styles, cultureInfo),
                System.UInt64.Parse(expPart, styles, cultureInfo)

        float decimal * 2.0 ** float exp

    let decimalNumber str =
        let styles = NumStyles.AllowDecimalPoint
                 ||| NumStyles.AllowExponent
                 ||| NumStyles.AllowTrailingSign
        System.Double.Parse(str, styles, cultureInfo)


    let number s =
        let str = consumeValue s
        let num =
            if str.StartsWith("0x")  then
                hexNumber (str.Substring(2))
            else    
                decimalNumber str
        num |> Ast.Number

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

    let tableconstr s =
        failwith ""

    (* Parses an expr
       nil | false | true | Number | String | '...' | function |
       prefixexp | tableconstr | expr binaryop expr | unaryop expr *)
    let rec expr s =
        // Terminals
        let rec simpleExpr () =
            match symbol s with
            | S.Nil ->
                consume s
                Ast.Nil
            | S.False ->
                consume s
                Ast.Boolean false
            | S.True ->
                consume s
                Ast.Boolean true
            | S.Number ->
                 number s
            | S.String ->
                Ast.String (consumeValue s)
            | S.DotDotDot ->
                consume s
                Ast.VarArgs
            | S.Function ->
                Ast.FuncExpr (function' s)
            | S.Identifier | S.LeftParen ->
                Ast.PrefixExpr (prefixExpr s)
            | sym when isUnaryOp sym ->
                let op = unaryOp s
                let expr = binaryExpr (simpleExpr()) unaryOpPriority
                Ast.UnaryOp (op, expr)
            | sym ->
                failparserUnexpected s sym

        // Recurse while we have higher binding
        and binaryExpr left limit =
            if isBinaryOp (symbol s) && (leftBinaryPrio s > limit) then
                let prio = rightBinaryPrio s
                let op = binaryOp s
                let right = binaryExpr (simpleExpr()) prio
                Ast.BinaryOp (left, op, right)
            else
                left
        
        // Left associative recursion
        let rec expr left =
            let binaryExpr = binaryExpr left 0
            if isBinaryOp (symbol s) then
                expr binaryExpr
            else
                binaryExpr

        expr (simpleExpr())

    (* Parses a prefixexpr, bottom-up parsing
       Finds the terminals Name or '(', if '(' is found parse an expr,
       if Name is found parse from bottom up into a functioncall or var
       prefixexp ::= var | functioncall | '(' exp ')'
       var ::= Name | prefixexp '[' exp ']' | prefixexp '.' Name
       functioncall ::= prefixexp args | prefixexp ':' Name args *)
    and prefixExpr s =
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
            // Args
            | S.LeftParen | S.LeftBrack | S.String ->
                Ast.FuncCallNormal (leftAst, args s) |> Ast.FuncCall |> prefixExpr

            // Unrecognized, return what we have
            | _ ->
                leftAst

        // Parse the terminal/bottom symbol of the prefix expression
        match symbol s with
        | S.Identifier ->
            consumeValue s |> Ast.Name |> Ast.VarExpr |> prefixExpr
        | S.LeftParen ->
            consume s
            let pexpr = expr s |> Ast.Expr |> prefixExpr
            expect s S.RightParen
            pexpr
        | sym ->
            failparserUnexpected s sym

    (* Parses an exprlist
       expr {',' expr} *)
    and exprlist s =
        let rec exprlist exprs =
            if symbol s = S.Comma then
                consume s
                exprlist (expr s :: exprs)
            else
                exprs
        List.rev (exprlist [expr s])

    (* Parses args
       '(' [explist] ')' | '{' [fieldlist] '}' | String *)
    and args s =
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

    (* Parses a varlist, starts at first comma after parsing one var
       ',' var {',' var} *)
    let rec varlist s vars =
        consume s
        let preexpr = prefixExpr s

        if symbol s = S.Comma then
            match liftVarExpr preexpr with
            | Some var -> varlist s (var :: vars)
            | None     -> failparserUnexpected s S.Comma
        else
            List.rev vars

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
        // Statements
        | S.Do -> Left  <| do' s
        | S.While -> Left  <| while' s
        | S.Repeat -> Left  <| repeat s
        | S.If -> Left  <| if' s
        | S.For -> Left  <| for' s
        | S.Function -> Left  <| function' s
        | S.Local -> Left  <| local s
        // Assignment or function call starts with terminals Name or '('
        | S.Identifier | S.LeftParen -> Left  <| assignOrFunccall s
        // Last statements
        | S.Return -> Right <| (Ast.Return <| exprlist s)
        | S.Break -> Right <| Ast.Break
        // Unexpected
        | sym -> failparserUnexpected s sym

    (* Parses a block
       {stat [';']} [laststat [';']] *)
    let block s =
        let rec block statements =
            if symbol s = S.EOF then
                (List.rev statements, None)
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
                    (List.rev statements, Some lastStat)

        block []
                

    let parse source : Ast.Block =
        let lexer = Lexer.create source
        let s = State(lexer)
        consume s
        block s
