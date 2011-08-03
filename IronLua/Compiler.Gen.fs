namespace IronLua.Compiler

module internal Gen =
    module private Compiler =
        let rec block scope (stats, lastStat) =
            let statExprs = List.map (statement scope) stats
            let lastStatExpr = lastStatement scope lastStat
            Dlr.block statExprs lastStatExpr

        and statement scope stat =
            match stat with
            | Ast.Assign(vars, exprs) ->
                assign scope vars exprs
            | Ast.StatFuncCall funccall ->
                statFuncCall scope funccall
            | Ast.Do block' ->
                do' scope block
            | Ast.While(test, block') ->
                while' scope test block'
            | Ast.Repeat(block', test) ->
                repeat scope block' test
            | Ast.If(test, block', elseifs, else') ->
                if' scope test block' elseifs else'
            | Ast.For(name, var, limit, step, block') ->
                for' scope name var limit step block'
            | Ast.ForIn(names, exprs, block') ->
                forin scope names exprs block'
            | Ast.Func(funcname, funcbody) ->
                func scope funcname funcbody
            | Ast.LocalFunc(name, funcbody) ->
                localFunc scope name funcbody
            | Ast.LocalAssign(names, exprs) ->
                localAssign scope names exprs

        and lastStatement scope stat =
            match stat with
            | Ast.Return exprs ->
                return' scope exprs
            | Ast.Break ->
                break' scope

        and assign scope vars exprs =
            failwith ""

        and statFuncCall scope funccall =
            failwith ""

        and do' scope block =
            failwith ""

        and while' scope test block' =
            failwith ""

        and repeat scope block' test =
            failwith ""

        and if' scope test block' elseifs else' =
            failwith ""
        
        and for' scope name var limit step block' =
            failwith ""

        and forin scope names exprs block' =
            failwith ""

        and func scope funcname funcbody =
            failwith ""

        and localFunc scope name funcbody =
            failwith ""

        and localAssign scope names exprs =
            failwith ""

        and return' scope exprs =
            failwith ""

        and break' scope =
            failwith ""
        

    let compile ast =
        Compiler.block (Scope.createRoot ()) ast