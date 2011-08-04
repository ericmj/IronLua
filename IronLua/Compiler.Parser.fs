namespace IronLua.Compiler

open IronLua.Error

module internal Parser =
    type S = Lexer.Symbol
    type UnaryOp = Ast.UnaryOp
    type BinaryOp = Ast.BinaryOp
    type NumStyles = System.Globalization.NumberStyles

    let cultureInfo = System.Globalization.CultureInfo.InvariantCulture


    module private State =
        type T =
            val File : string
            val Lexer : unit -> Lexer.Lexeme
            val mutable Lexeme : Lexer.Lexeme
            val mutable LastLexeme : Lexer.Lexeme
            val mutable NextLexeme : Lexer.Lexeme

            new(lexer) = {
                File = "<unknown>"
                Lexer = lexer
                Lexeme = Unchecked.defaultof<Lexer.Lexeme>
                LastLexeme = Unchecked.defaultof<Lexer.Lexeme>
                NextLexeme = lexer()
            }

        let inline failparser (s:T) msg =
            let (_, _, line, column) = s.Lexeme
            raise <| CompileError(s.File, (line, column), msg)

        let inline failparserNear (s:T) msg =
            let (sym, _, _, _) = s.Lexeme
            failparser s <| msg (Lexer.prettySymbol sym)

        let inline failparserExpected (s:T) sym1 sym2 =
            let msg = Message.expectedSymbol (Lexer.prettySymbol sym1) (Lexer.prettySymbol sym2)
            failparser s msg

        let inline failparserUnexpected (s:T) sym =
            Lexer.prettySymbol sym |> Message.unexpectedSymbol |> failparser s
    
        let inline create lexer =
            T(lexer)

        let inline symbol (s:T) =
            let (sym, _, _, _) = s.Lexeme
            sym

        let inline peekSymbol (s:T) =
            let (symbol, _, _, _) = s.NextLexeme
            symbol

        let inline consume (s:T) =
            s.LastLexeme <- s.Lexeme
            match s.NextLexeme with
            | (S.EOF, _, _, _) ->
                s.Lexeme <- s.NextLexeme
            | lexeme -> 
                s.Lexeme <- s.NextLexeme
                s.NextLexeme <- s.Lexer()

        let inline tryConsume (s:T) sym =
            let tok = symbol s
            if tok = sym then consume s

        let inline expect (s:T) sym =
            let sym2 = symbol s
            if sym2 = sym
                then consume s
                else failparserExpected s sym2 sym

        let inline consumeValue (s:T) =
            let (_, value, _, _) = s.Lexeme
            consume s
            value

        let inline expectValue (s:T) sym =
            let (_, value, _, _) = s.Lexeme
            expect s sym
            value

        let inline line (s:T) =
            let (_, _, line, _) = s.Lexeme
            line

        let inline lastLine (s:T) =
            let (_, _, line, _) = s.LastLexeme
            line


    open State

    module private Util =
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
            | S.TildeEqual | S.EqualEqual -> (3, 3)
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
        
        // Tries to lift Ast.Name from Ast.Expr
        let liftName expr =
            let liftName var =
                match var with
                | Some (Ast.Name name) -> Some name
                | _ -> None

            match expr with
            | Ast.PrefixExpr prefixexpr ->
                prefixexpr |> liftVarExpr |> liftName
            | _ -> None

        // Parses a hexadecimal number, not including starting '0x'
        let hexNumber (str: string) =
            let styles = NumStyles.AllowHexSpecifier

            // Split integer and exponent part and parse seperately
            let expIndex = str.IndexOfAny([|'p'; 'P'|])
            if expIndex = -1 then
                float <| System.UInt64.Parse(str, styles, cultureInfo)
            else
                let decimalPart, expPart = str.Substring(0, expIndex), str.Substring(expIndex+1)
                let decimal = float <| System.UInt64.Parse(decimalPart, styles, cultureInfo)
                let exponent = float <| System.UInt64.Parse(expPart, styles, cultureInfo)
                decimal * 2.0 ** exponent

        // Parses decimal number, integer or floating point including exponent
        let decimalNumber str =
            let styles = NumStyles.AllowDecimalPoint
                     ||| NumStyles.AllowExponent
                     ||| NumStyles.AllowTrailingSign
            System.Double.Parse(str, styles, cultureInfo)


    open Util

    module private Parser =
        (* Parses a unary operation *)
        let unaryOp s =
            let sym = symbol s
            consume s
            match sym with
            | S.Minus -> UnaryOp.Negative
            | S.Not   -> UnaryOp.Not
            | S.Hash  -> UnaryOp.Length
            | _       -> Unchecked.defaultof<UnaryOp>

        (* Parses a binary operation *)
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
            | S.EqualEqual   -> BinaryOp.EqualEqual
            | S.DotDot       -> BinaryOp.Concat
            | S.Plus         -> BinaryOp.Add
            | S.Minus        -> BinaryOp.Subtract
            | S.Star         -> BinaryOp.Multiply
            | S.Slash        -> BinaryOp.Divide
            | S.Percent      -> BinaryOp.Mod
            | S.Carrot       -> BinaryOp.Raise
            | _              -> Unchecked.defaultof<BinaryOp>

        (* Parses a number *)
        let number s =
            let str = expectValue s S.Number
            let num =
                if str.StartsWith("0x")  then
                    hexNumber (str.Substring(2))
                else    
                    decimalNumber str
            num |> Ast.Number

        (* Parses a namelist
           Name {sep Name} *)
        let namelist s sep =
            let rec namelist names =
                if symbol s = sep then
                    consume s
                    if symbol s = S.Identifier
                        then namelist (consumeValue s :: names)
                        else names
                else
                    names
            List.rev (namelist [expectValue s S.Identifier])

        (* Parses a funcname
           Name {'.' Name} [':' Name] *)
        let funcname s =
            let names = namelist s S.Dot
            let name3 =
                match symbol s with
                | S.Colon -> consume s; Some (expectValue s S.Identifier)
                | _       -> None

            (names, name3)

        (* Parses a parlist
           Name {',' Name} [',' '...'] | '...' *)
        let parlist s =
            let rec parlist names =
                if symbol s = S.Comma then
                    consume s
                    match symbol s with
                    | S.Identifier -> parlist (consumeValue s :: names)
                    | S.DotDotDot  -> consume s; (List.rev names, true)
                    | sym          -> failparserUnexpected s sym
                else
                    (List.rev names, false)

            match symbol s with
            | S.DotDotDot -> ([], true)
            | _           -> parlist [expectValue s S.Identifier]

        (* Parses a do statement
           'do' block 'end' *)
        let rec do' s =
            expect s S.Do
            let block' = block s
            expect s S.End
            Ast.Do block'

        (* Parses a while statement
           'while' expr 'do' block 'end' *)
        and while' s =
            expect s S.While
            let test = expr s
            expect s S.Do
            let block' = block s
            expect s S.End
            Ast.While (test, block')

        (* Parses a repeat statement 
           'repeat' block 'until' expr *)
        and repeat s =
            expect s S.Repeat
            let block' = block s
            expect s S.Until
            let test = expr s
            Ast.Repeat (block', test)

        (* Parses an if statement 
           'if' expr 'then' block {'elseif' expr then 'block'} ['else' block] 'end' *)
        and if' s =
            let rec elseifs elifs =
                if symbol s = S.Elseif then
                    consume s
                    let test = expr s
                    expect s S.Then
                    let block' = block s
                    elseifs ((test, block') :: elifs)
                else
                    elifs

            let else' () =
                if symbol s = S.Else then
                    consume s
                    Some (block s)
                else
                    None

            expect s S.If
            let test = expr s
            expect s S.Then
            let block' = block s 
            let elifs = List.rev (elseifs [])
            let elseBlock = else'()
            expect s S.End

            Ast.If (test, block', elifs, elseBlock)

        (* Parses a for statement 
           'for' name '=' expr ',' expr [',' expr] 'do' block 'end' |
           'for' namelist 'in' exprlist 'do' block 'end' *)
        and for' s =
            let for' name = 
                consume s
                let expr1 = expr s
                expect s S.Comma
                let expr2 = expr s
                let expr3 =
                    match symbol s with
                    | S.Comma -> consume s; Some(expr s)
                    | _       -> None
                expect s S.Do
                let block' = block s
                expect s S.End
                Ast.For (name, expr1, expr2, expr3, block')

            let forin name =
                let names = namelist s S.Comma
                expect s S.In
                let exprs = exprlist s
                expect s S.Do
                let block' = block s
                expect s S.End
                Ast.ForIn (names, exprs, block')

            expect s S.For
            let name = expectValue s S.Identifier
            match symbol s with
            | S.Equal        -> for' name
            | S.Comma | S.In -> forin name
            | sym            -> failparserUnexpected s sym

        (* Parses a funcbody
           '(' [exprlist] ')' block 'end' *)
        and funcbody s =
            expect s S.LeftParen
            let parlist' =
                match symbol s with
                | S.RightParen -> ([], false)
                | _            -> parlist s
            expect s S.RightParen
            let block' = block s
            expect s S.End
            (parlist', block')

        (* Parses a function statement
           'function' funcname funcbody *)
        and function' s =
            expect s S.Function
            Ast.Func (funcname s, funcbody s)

        (* Parses a local statement
           'local' 'function' name funcbody |
           'local' namelist ['=' exprlist] *)
        and local s =
            expect s S.Local
            match symbol s with
            | S.Function ->
                consume s
                let name = expectValue s S.Identifier
                let funcbody' = funcbody s
                Ast.LocalFunc (name, funcbody')
            | S.Identifier ->
                let names = namelist s S.Comma
                let exprs =
                    match symbol s with
                    | S.Equal -> consume s; exprlist s
                    | _       -> []
                Ast.LocalAssign(names, exprs)
            | sym ->
                failparserUnexpected s sym

        (* Parses a field
           '[' expr ']' '=' expr | Name '=' expr | expr *)
        and field s =
            match symbol s with
            | S.LeftBrack ->
                consume s
                let index = expr s
                expect s S.RightBrack
                expect s S.Equal
                let value = expr s
                Ast.FieldExprAssign (index, value)
            | _ ->
                let exp = expr s
                // If '=' is found, exp should be a Name
                if symbol s = S.Equal then
                    consume s
                    match liftName exp with
                    | Some name -> Ast.FieldNameAssign (name, expr s)
                    | None      -> failparserUnexpected s S.Equal
                else
                    Ast.FieldExpr exp

        (* Parses a tableconstr
           [field {fieldsep field} [fieldsep]] *)
        and fieldlist s =
            let rec fieldlist fields =
                // We know fieldlist is inside a tableconstr which ends with a '}'
                if symbol s = S.RightBrace then
                    fields
                else
                    let fields' = (field s :: fields)
                    match symbol s with
                    | S.Comma | S.SemiColon ->
                        consume s
                        fieldlist fields'
                    | S.RightBrace ->
                        fields'
                    | sym ->
                        failparserUnexpected s sym

            List.rev (fieldlist [])

        (* Parses a tableconstr
           '{' fieldlist '}' *)
        and tableconstr s =
            consume s
            let fields = fieldlist s
            expect s S.RightBrace 
            fields

        (* Parses an expr
           nil | false | true | Number | String | '...' | function |
           prefixexp | tableconstr | expr binaryop expr | unaryop expr *)
        and expr s =
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
                    consume s
                    Ast.FuncExpr (funcbody s)
                | S.Identifier | S.LeftParen ->
                    Ast.PrefixExpr (prefixExpr s)
                | S.LeftBrace ->
                    Ast.TableConstr (tableconstr s)
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
                    Ast.TableDot (leftAst, expectValue s S.Identifier) |> Ast.VarExpr |> prefixExpr

                // Function call
                | S.Colon ->
                    consume s
                    Ast.FuncCallObject (leftAst, expectValue s S.Identifier, args s) |> Ast.FuncCall |> prefixExpr
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
           '(' [explist] ')' | tableconstr | String *)
        and args s =
            match symbol s with
            | S.LeftParen ->
                if line s <> lastLine s then
                    failparserNear s Message.ambiguousFuncCall
                consume s
                let exprs = exprlist s |> Ast.ArgsNormal 
                expect s S.RightParen
                exprs
            | S.LeftBrace ->
                tableconstr s |> Ast.ArgsTable
            | S.String ->
                consumeValue s |> Ast.ArgString
            | sym ->
                failparserUnexpected s sym

        (* Parses a varlist, starts at first comma after parsing one var
           ',' var {',' var} *)
        and varlist s vars =
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
        and assignOrFunccall s =
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

        (* Parses a block
           {statement [';']} [laststatement [';']] *)
        and block s =
            let rec statements stats =
                // Watch out for raptors while editing this try/finally
                try
                    match symbol s with
                    // Statements
                    | S.Do -> statements (do' s :: stats)
                    | S.While -> statements (while' s :: stats)
                    | S.Repeat -> statements (repeat s :: stats)
                    | S.If -> statements (if' s :: stats)
                    | S.For -> statements (for' s :: stats)
                    | S.Function -> statements (function' s :: stats)
                    | S.Local -> statements (local s :: stats)
                    // Assignment or function call starts with terminals Name or '('
                    | S.Identifier | S.LeftParen -> statements (assignOrFunccall s :: stats)
                    // Last statements
                    | S.Return -> (stats, exprlist s |> Ast.Return |> Some)
                    | S.Break -> (stats, Some Ast.Break)
                    // Unexpected
                    | sym -> (stats, None)
                finally
                     tryConsume s S.SemiColon

            statements []
                

    let parse source : Ast.Block =
        let lexer = Lexer.create source
        let s = State.create lexer
        consume s
        let block' = Parser.block s
        expect s S.EOF
        block'
