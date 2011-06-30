namespace IronLua.Compiler

module Ast =
    type Name = string

    type BinaryOp
        = Or = 10
        | And = 20
        | LessThan = 30
        | GreaterThan = 31
        | LessThanEquals = 32
        | GreaterThanEquals = 33
        | NotEqual = 34
        | Equal = 35
        | Concat = 40
        | Add = 50
        | Subtract = 51
        | Multiply = 52
        | Divide = 53
        | Mod = 54
        | Raised = 70

    type UnaryOp
        = Not = 60
        | Hash = 61
        | Negative = 62

    type Block
        = Statements of Statement list * LastStatement option

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
        | True
        | False
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