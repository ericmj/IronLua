using System;

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
    }
}
