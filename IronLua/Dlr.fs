namespace IronLua

open System.Linq.Expressions

type Expr = Expression
type ParamExpr = ParameterExpression

module Dlr =
    let null' : Expr = upcast Expr.Constant(null)
    let const' v : Expr = upcast Expr.Constant(v)

    let block (params':ParamExpr seq) stats : Expr = upcast Expr.Block(params', stats)
    let simpleBlock stats : Expr = upcast Expr.Block(stats)

    let var () = Expr.Variable(typeof<obj>)
    let vars n = Array.init n (fun _ -> var())

    let assign var expr : Expr = upcast Expr.Assign(var, expr)

    let dynamic binder (es:Expr seq) : Expr = upcast Expr.Dynamic(binder, typeof<obj>, es)
    let dynamic2 binder e1 e2 : Expr = upcast Expr.Dynamic(binder, typeof<obj>, e1, e2)
