namespace IronLua.Compiler

open System.Collections.Generic

module Scope =
    type T =
        val Parent : T option
        val Children : T list
        val Variables : Dictionary<string, ParamExpr>

        new(parent) = {
            Parent = parent
            Children = []
            Variables = new Dictionary<string, ParamExpr>()
        }

    let createRoot () =
        T(None)

    let create parent =
        T(Some parent)
        

