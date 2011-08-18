using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using IronLua.Compiler;
using IronLua.Compiler.Ast;
using IronLua.Util;
using Expr = System.Linq.Expressions.Expression;

namespace IronLua.Compiler
{
    class Generator : IStatementVisitor<Expr>, ILastStatementVisitor<Expr>, IExpressionVisitor<Expr>
    {
        Scope scope;

        public Generator()
        {
        }

        public Expr Compile(Block block)
        {
            scope = new Scope();
            return Visit(block);
        }

        Expr Visit(Block block)
        {
            var linqStatements = block.Statements.Select(s => s.Visit(this));

            if (block.LastStatement != null)
                linqStatements = linqStatements.Add(block.LastStatement.Visit(this));

            return Expr.Block(linqStatements);
        }

        Expr IStatementVisitor<Expr>.Visit(Statement.Assign statement)
        {
            throw new NotImplementedException();
        }

        Expr IStatementVisitor<Expr>.Visit(Statement.Do statement)
        {
            throw new NotImplementedException();
        }

        Expr IStatementVisitor<Expr>.Visit(Statement.For statement)
        {
            throw new NotImplementedException();
        }

        Expr IStatementVisitor<Expr>.Visit(Statement.ForIn statement)
        {
            throw new NotImplementedException();
        }

        Expr IStatementVisitor<Expr>.Visit(Statement.Function statement)
        {
            throw new NotImplementedException();
        }

        Expr IStatementVisitor<Expr>.Visit(Statement.FunctionCall statement)
        {
            throw new NotImplementedException();
        }

        Expr IStatementVisitor<Expr>.Visit(Statement.If statement)
        {
            throw new NotImplementedException();
        }

        Expr IStatementVisitor<Expr>.Visit(Statement.LocalAssign statement)
        {
            // Assign values to temporaries
            var valuesCompiled = statement.Values.Select(val => val.Visit(this)).ToList();
            var tempVariables = valuesCompiled.Select(expr => Expr.Variable(expr.Type)).ToList();
            var tempAssigns = tempVariables.Zip(valuesCompiled, Expr.Assign);

            // Shrink or pad temporary's list with nil to match local's list length
            // and cast temporaries to locals type
            var locals = statement.Identifiers.Select(scope.AddLocal).ToList();
            var tempVariablesResized = tempVariables
                .Resize(statement.Identifiers.Count, new Expression.Nil().Visit(this))
                .Zip(locals, (tempVar, local) => Expr.Convert(tempVar, local.Type));

            // Assign temporaries to locals
            var realAssigns = locals.Zip(tempVariablesResized, Expr.Assign);
            return Expr.Block(tempVariables.Concat(locals), tempAssigns.Concat(realAssigns));
        }

        Expr IStatementVisitor<Expr>.Visit(Statement.LocalFunction statement)
        {
            throw new NotImplementedException();
        }

        Expr IStatementVisitor<Expr>.Visit(Statement.Repeat statement)
        {
            throw new NotImplementedException();
        }

        Expr IStatementVisitor<Expr>.Visit(Statement.While statement)
        {
            throw new NotImplementedException();
        }

        Expr ILastStatementVisitor<Expr>.Visit(LastStatement.Break lastStatement)
        {
            throw new NotImplementedException();
        }

        Expr ILastStatementVisitor<Expr>.Visit(LastStatement.Return lastStatement)
        {
            throw new NotImplementedException();
        }

        Expr IExpressionVisitor<Expr>.Visit(Expression.BinaryOp expression)
        {
            throw new NotImplementedException();
        }

        Expr IExpressionVisitor<Expr>.Visit(Expression.Boolean expression)
        {
            return Expr.Constant(expression.Literal);
        }

        Expr IExpressionVisitor<Expr>.Visit(Expression.Function expression)
        {
            throw new NotImplementedException();
        }

        Expr IExpressionVisitor<Expr>.Visit(Expression.Nil expression)
        {
            return Expr.Default(typeof(object));
        }

        Expr IExpressionVisitor<Expr>.Visit(Expression.Number expression)
        {
            return Expr.Constant(expression.Literal);
        }

        Expr IExpressionVisitor<Expr>.Visit(Expression.Prefix expression)
        {
            throw new NotImplementedException();
        }

        Expr IExpressionVisitor<Expr>.Visit(Expression.String expression)
        {
            return Expr.Constant(expression.Literal);
        }

        Expr IExpressionVisitor<Expr>.Visit(Expression.Table expression)
        {
            throw new NotImplementedException();
        }

        Expr IExpressionVisitor<Expr>.Visit(Expression.UnaryOp expression)
        {
            throw new NotImplementedException();
        }

        Expr IExpressionVisitor<Expr>.Visit(Expression.Varargs expression)
        {
            throw new NotImplementedException();
        }
    }
}
