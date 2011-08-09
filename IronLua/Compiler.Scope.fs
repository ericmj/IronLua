namespace IronLua.Compiler

open System.Collections.Generic

module internal Scope =
    type T =
        val Parent : T option
        val Children : T list
        val Variables : Dictionary<string, ParamExpr>
        val Globals : Dictionary<string, ParamExpr>

        new(parent) = {
            Parent = parent
            Children = []
            Variables = new Dictionary<string, ParamExpr>()
            Globals =
                match parent with
                | Some p -> p.Globals
                | None   -> new Dictionary<string, ParamExpr>()
        }

    let createRoot () =
        T(None)

    let create parent =
        T(Some parent)

    let rec findId (s:T) id =
        let mutable paramExpr = Unchecked.defaultof<ParamExpr>
        if s.Variables.TryGetValue(id, &paramExpr) then
            Some paramExpr
        else
            match s.Parent with
            | Some parent ->
                findId parent id
            | None ->
                if s.Globals.TryGetValue(id, &paramExpr)
                    then Some paramExpr
                    else None

    let addLocal (s:T) id =
        let var = Dlr.var()
        s.Variables.Add(id, var)
        var

    let addGlobal (s:T) id =
        let var = Dlr.var()
        s.Globals.Add(id, var)
        var
        

