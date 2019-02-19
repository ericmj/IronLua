namespace IronLua.Compiler.Ast
{
    static class AstExtensions
    {
        public static Variable LiftVariable(this PrefixExpression prefixExpression)
        {
            var varPrefixExpr = prefixExpression as PrefixExpression.Variable;
            return varPrefixExpr != null ? varPrefixExpr.Var : null;
        }

        public static FunctionCall LiftFunctionCall(this PrefixExpression prefixExpression)
        {
            var funcCallPrefixExpr = prefixExpression as PrefixExpression.FunctionCall;
            return funcCallPrefixExpr != null ? funcCallPrefixExpr.Call : null;
        }

        public static string LiftIdentifier(this Expression expression)
        {
            var prefixExpression = expression as Expression.Prefix;
            if (prefixExpression == null)
                return null;

            var variablePrefixExpr = prefixExpression.Expression as PrefixExpression.Variable;
            if (variablePrefixExpr == null)
                return null;

            var identifierVariable = variablePrefixExpr.Var as Variable.Identifier;
            return identifierVariable != null ? identifierVariable.Value : null;
        }

        public static bool IsVarargs(this Expression expression)
        {
            if (expression is Expression.Varargs)
                return true;

            return false;
        }

        public static bool IsFunctionCall(this Expression expression)
        {
            Expression.Prefix exprPrefix;
            PrefixExpression.Expression prefixExpr;

            if ((exprPrefix = expression as Expression.Prefix) == null)
                return false;
            if (exprPrefix.Expression is PrefixExpression.FunctionCall)
                return true;

            if ((prefixExpr = exprPrefix.Expression as PrefixExpression.Expression) == null)
                return false;
            return IsFunctionCall(prefixExpr.Expr);
        }
    }
}
