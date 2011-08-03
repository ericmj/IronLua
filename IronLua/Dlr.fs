namespace IronLua.Compiler

open System.Linq.Expressions

type Expr = Expression
type ParamExpr = ParameterExpression

module Dlr =
    let expr expr = expr :> Expr
    let exprs exprs = Array.map expr exprs

    let void' = Expr.Empty()

    let block stats lastStat =
        let all =
            List.toArray stats
         |> Array.append [| lastStat; void' |]
         |> exprs

        Expr.Block(all)
