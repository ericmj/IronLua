namespace IronLua.Compiler

module Ast =
    type Name = string

    type BinaryOp
        = Or            = 0
        | And           = 1
        | Less          = 2
        | Greater       = 3
        | LessEquals    = 4
        | GreaterEquals = 5
        | NotEqual      = 6
        | Equal         = 7
        | Concat        = 8
        | Add           = 9
        | Subtract      = 10
        | Multiply      = 11
        | Divide        = 12
        | Mod           = 13
        | Raised        = 14

    type UnaryOp
        = Not           = 15
        | Hash          = 16
        | Negative      = 17

    type Block
        = Block of Statement list * LastStatement option

    and Statement
        = Assign of Var list * Expr list
        | FuncCall of FuncCall
        | Do of Block
        | While of Expr * Block
        | Repeat of Block * Expr
        | If of Expr * Block * (Expr * Block) list * Block option  
        | For of Name * Expr * Expr * Expr list * Block
        | ForIn of Name list * Expr list * Block
        | Func of FuncName * FuncBody
        | LocalFunc of Name * FuncBody
        | LocalAssign of Name list * Expr list

    and LastStatement
        = Return of Expr list
        | Break

    and FuncName
        = FuncName of Name * Name list * Name

    and Var
        = VarName of Name
        | TableEntry of PrefixExpr * Expr
        | TableDot of PrefixExpr * Name
    
    and Expr
        = Nil
        | Boolean of bool
        | Number of double
        | String of string
        | VarArgs
        | Func of FuncBody
        | PrefixExpr of PrefixExpr
        | TableConstructor of Field * Field list
        | BinaryOpExpr of Expr * BinaryOp * Expr
        | UnaryOpExpr of UnaryOp * Expr
    
    and PrefixExpr
        = VarExpr of Var
        | FuncCall of FuncCall
        | Expr of Expr
    
    and FuncCall
        = FuncCallNormal of PrefixExpr * Args
        | FuncCallObject of PrefixExpr * Name * Args

    and Args
        = ArgsNormal of Expr list
        | ArgsTable of Field list
        | ArgString of string

    and FuncBody = ParamList * Block

    and ParamList = Name list * bool // varargs

    and Field
        = FieldExpr of Expr * Expr
        | FieldName of Name * Expr
        | FieldOnly of Expr