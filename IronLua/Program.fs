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
        | Plus = 50
        | MinusBinary = 51
        | Star = 52
        | Slash = 53
        | Percent = 54
        | Roof = 70

    type UnaryOp
        = Not = 60
        | Hash = 61
        | MinusUnary = 62

    type Block
        = Statements of Statement list * LastStatement

    and Statement
        = Assignment of Var list * Expression list
        | FunctionCall of FunctionCall
        | Do of Block
        | While of Expression * Block
        | Repeat of Block * Expression
        | If of Expression * Block * (Expression * Block) list * Block option  
        | For of Name * Expression * Expression * Expression list * Block
        | ForIn of Name list * Expression list * Block
        | Function of FuncName * FuncBody
        | LocalFunction of Name * FuncBody
        | LocalAssignment of Name list * Expression list

    and LastStatement
        = Return of Expression list
        | Break

    and FuncName
        = FuncName of Name * Name list * Name

    and Var
        = VarName of Name
        | TableEntry of PrefixExpression * Expression
        | TableDot of PrefixExpression * Name
    
    and Expression
        = Nil
        | True
        | False
        | Number of double
        | String of string
        | VarArgs
        | Function of FuncBody
        | PrefixExpression of PrefixExpression
        | TableConstructor of Field * Field list
        | BinaryOpExpr of Expression * BinaryOp * Expression
        | UnaryOpExpr of UnaryOp * Expression
    
    and PrefixExpression
        = VarExpr of Var
        | FunctionCall of FunctionCall
        | Expression of Expression
    
    and FunctionCall
        = FunctionCallNormal of PrefixExpression * Arguments
        | FunctionCallObject of PrefixExpression * Name * Arguments

    and Arguments
        = ArgumentsNormal of Expression list
        | ArgumentsTable of Field list
        | ArgumentString of string

    and FuncBody = ParameterList * Block

    and ParameterList = Name list * bool // varargs

    and Field
        = FieldExpression of Expression * Expression
        | FieldName of Name * Expression
        | FieldOnly of Expression