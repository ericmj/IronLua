namespace IronLua.Compiler.Ast
{
    interface IArgumentsVisitor<out T>
    {
        T Visit(Arguments.Normal arguments);
        T Visit(Arguments.String arguments);
        T Visit(Arguments.Table arguments);
    }

    interface IExpressionVisitor<out T>
    {
        T Visit(Expression.BinaryOp expression);
        T Visit(Expression.Boolean expression);
        T Visit(Expression.Function expression);
        T Visit(Expression.Nil expression);
        T Visit(Expression.Number expression);
        T Visit(Expression.Prefix expression);
        T Visit(Expression.String expression);
        T Visit(Expression.Table expression);
        T Visit(Expression.UnaryOp expression);
        T Visit(Expression.Varargs expression);
    }

    interface IFieldVisitor<out T>
    {
        T Visit(Field.MemberExpr field);
        T Visit(Field.MemberId field);
        T Visit(Field.Normal field);
    }

    interface IFunctionCallVisitor<out T>
    {
        T Visit(FunctionCall.Normal functionCall);
        T Visit(FunctionCall.Table functionCall);
    }

    interface IPrefixExpressionVisitor<out T>
    {
        T Visit(PrefixExpression.Expression prefixExpr);
        T Visit(PrefixExpression.FunctionCall prefixExpr);
        T Visit(PrefixExpression.Variable prefixExpr);
    }

    interface IStatementVisitor<out T>
    {
        T Visit(Statement.Assign statement);
        T Visit(Statement.Do statement);
        T Visit(Statement.For statement);
        T Visit(Statement.ForIn statement);
        T Visit(Statement.Function statement);
        T Visit(Statement.FunctionCall statement);
        T Visit(Statement.If statement);
        T Visit(Statement.LocalAssign statement);
        T Visit(Statement.LocalFunction statement);
        T Visit(Statement.Repeat statement);
        T Visit(Statement.While statement);
        T Visit(Statement.Goto statement);
        T Visit(Statement.LabelDecl statement);

        T Visit(LastStatement.Break lastStatement);
        T Visit(LastStatement.Return lastStatement);
    }

    interface IVariableVisitor<out T>
    {
        T Visit(Variable.Identifier variable);
        T Visit(Variable.MemberExpr variable);
        T Visit(Variable.MemberId variable);
    }
}
