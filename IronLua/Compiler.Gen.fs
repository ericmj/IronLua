namespace IronLua.Compiler

open IronLua
open IronLua.Helper

module internal Gen =
    module private Compiler =
        let rec block s (stats, lastStat) : Expr =
            let allStats =
                   List.map (statement s)
                |> List.append [ lastStatement s lastStat ]

            Dlr.simpleBlock allStats

        and statement s stat : Expr =
            match stat with
            | Ast.Assign(vars, exprs) ->
                assign s vars exprs
            | Ast.StatFuncCall funccall ->
                statFuncCall s funccall
            | Ast.Do block' ->
                do' s block
            | Ast.While(test, block') ->
                while' s test block'
            | Ast.Repeat(block', test) ->
                repeat s block' test
            | Ast.If(test, block', elseifs, else') ->
                if' s test block' elseifs else'
            | Ast.For(name, var, limit, step, block') ->
                for' s name var limit step block'
            | Ast.ForIn(names, exprs, block') ->
                forin s names exprs block'
            | Ast.Func(funcname, funcbody) ->
                func s funcname funcbody
            | Ast.LocalFunc(name, funcbody) ->
                localFunc s name funcbody
            | Ast.LocalAssign(names, exprs) ->
                localAssign s names exprs

        and lastStatement s stat : Expr =
            match stat with
            | Ast.Return exprs ->
                return' s exprs
            | Ast.Break ->
                break' s

        and assign s vars exprs : Expr =
            let assign var value =
                match var with
                | Ast.Name name ->
                    match Scope.findId s name with
                    | Some paramExpr -> Dlr.assign paramExpr value
                    | None           -> Dlr.assign (Scope.addGlobal s name) value
                | Ast.TableEntry(prefix, index) ->
                    Dlr.dynamic (failwith "new CallInfo(1)") [prefixExpr s prefix; expr s index; value]
                | Ast.TableDot(prefix, name) ->
                    Dlr.dynamic2 (failwith name) (prefixExpr s prefix) value

            let tempVars = Dlr.vars exprs.Length
            let tempAssigns = exprs |> List.mapi (fun i e -> Dlr.assign tempVars.[i] (expr s e))
            Dlr.simpleBlock <|
                Array.map2 assign (List.toArray vars) (Array.resize<Expr> (Seq.cast<Expr> tempVars) (List.length vars) (Dlr.const' null))

        and statFuncCall s funccall : Expr =
            failwith ""

        and do' s block : Expr =
            failwith ""

        and while' s test block' : Expr =
            failwith ""

        and repeat s block' test : Expr =
            failwith ""

        and if' s test block' elseifs else' : Expr =
            failwith ""
        
        and for' s name var limit step block' : Expr =
            failwith ""

        and forin s names exprs block' : Expr =
            failwith ""

        and func s funcname funcbody : Expr =
            failwith ""

        and localFunc s name funcbody : Expr =
            failwith ""

        and localAssign s names exprs : Expr =
            failwith ""

        and return' s exprs : Expr =
            failwith ""

        and break' s : Expr =
            failwith ""

        and expr s exprs : Expr =
            failwith ""

        and prefixExpr s prefix : Expr =
            failwith ""
        

    let compile ast =
        Compiler.block (Scope.createRoot ()) ast