namespace IronLua.Compiler

module Ast =
    type Name = string

    type BinaryOp =
        | Or           = 0
        | And          = 1
        | Less         = 2
        | Greater      = 3
        | LessEqual    = 4
        | GreaterEqual = 5
        | NotEqual     = 6
        | Equal        = 7
        | Concat       = 8
        | Add          = 9
        | Subtract     = 10
        | Multiply     = 11
        | Divide       = 12
        | Mod          = 13
        | Raise        = 14
                       
    type UnaryOp =     
        | Not          = 15
        | Length       = 16
        | Negative     = 17

    type Block = Statement list * LastStatement option

    and Statement
        = Assign of Var list * Expr list
        | StatFuncCall of FuncCall
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
        = Name of Name
        | TableEntry of PrefixExpr * Expr
        | TableDot of PrefixExpr * Name
    
    and Expr
        = Nil
        | Boolean of bool
        | Number of double
        | String of string
        | VarArgs
        | FuncExpr of FuncBody
        | PrefixExpr of PrefixExpr
        | TableConstr of Field list
        | BinaryOp of Expr * BinaryOp * Expr
        | UnaryOp of UnaryOp * Expr
    
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
        = FieldExprAssign of Expr * Expr
        | FieldNameAssign of Name * Expr
        | FieldExpr of Expr