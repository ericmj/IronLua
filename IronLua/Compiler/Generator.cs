using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using IronLua.Compiler;
using IronLua.Compiler.Ast;
using IronLua.Runtime;
using IronLua.Runtime.Binder;
using IronLua.Util;
using Expr = System.Linq.Expressions.Expression;
using ParamExpr = System.Linq.Expressions.ParameterExpression;
using ExprType = System.Linq.Expressions.ExpressionType;
using Expression = IronLua.Compiler.Ast.Expression;
using IronLua.Library;

namespace IronLua.Compiler
{
    class Generator : IStatementVisitor<Expr>, ILastStatementVisitor<Expr>, IExpressionVisitor<Expr>,
                      IVariableVisitor<VariableVisit>, IPrefixExpressionVisitor<Expr>, IFunctionCallVisitor<Expr>,
                      IArgumentsVisitor<Expr[]>
    {
        static Dictionary<BinaryOp, ExprType> binaryExprTypes =
            new Dictionary<BinaryOp, ExprType>
                {
                    {BinaryOp.Or,           ExprType.OrElse},
                    {BinaryOp.And,          ExprType.AndAlso},
                    {BinaryOp.Equal,        ExprType.Equal},
                    {BinaryOp.NotEqual,     ExprType.NotEqual},
                    {BinaryOp.Less,         ExprType.LessThan},
                    {BinaryOp.Greater,      ExprType.GreaterThan},
                    {BinaryOp.LessEqual,    ExprType.LessThanOrEqual},
                    {BinaryOp.GreaterEqual, ExprType.GreaterThanOrEqual},
                    {BinaryOp.Add,          ExprType.Add},
                    {BinaryOp.Subtract,     ExprType.Subtract},
                    {BinaryOp.Multiply,     ExprType.Multiply},
                    {BinaryOp.Divide,       ExprType.Divide},
                    {BinaryOp.Mod,          ExprType.Modulo},
                    {BinaryOp.Power,        ExprType.Power}
                };

        static Dictionary<UnaryOp, ExprType> unaryExprTypes =
            new Dictionary<UnaryOp, ExprType>
                {
                    {UnaryOp.Negate, ExprType.Negate},
                    {UnaryOp.Not,    ExprType.Not}
                };

        Scope scope;
        Context context;

        public Generator(Context context)
        {
            this.context = context;
        }

        public Expression<Action> Compile(Block block)
        {
            var expr = Visit(block);
            return Expr.Lambda<Action>(expr);
        }

        Expr Visit(Block block)
        {
            var parentScope = scope;
            scope = scope == null ? Scope.CreateRoot() : Scope.CreateChild(parentScope);

            var statementExprs = block.Statements.Select(s => s.Visit(this)).ToList();
            if (block.LastStatement != null)
                statementExprs.Add(block.LastStatement.Visit(this));

            var expr = Expr.Block(scope.AllLocals(), statementExprs);
            scope = parentScope;
            return expr;
        }

        Expr IStatementVisitor<Expr>.Visit(Statement.Assign statement)
        {
            var variables = statement.Variables.Select(v => v.Visit(this)).ToList();

            // Try to wrap all values except the last with varargs select
            var values = new List<Expr>(statement.Values.Count);
            for (int i = 0; i < values.Count - 1; i++)
                values.Add(TryWrapWithVarargsSelect(statement.Values[i]));
            values.Add(statement.Values.Last().Visit(this));

            if (statement.Values.Last().IsVarargsOrFuncCall())
                return VarargsExpandAssignment(variables, values);

            // Assign values to temporaries
            var tempVariables = values.Select(expr => Expr.Variable(expr.Type)).ToList();
            var tempAssigns = tempVariables.Zip(values, Expr.Assign);

            // Shrink or pad temporary's list with nil to match variables's list length
            // and cast temporaries to object type
            var tempVariablesResized = tempVariables
                .Resize(statement.Variables.Count, new Expression.Nil().Visit(this))
                .Select(tempVar => Expr.Convert(tempVar, typeof(object)));

            // Assign temporaries to globals
            var realAssigns = variables.Zip(tempVariablesResized, Assign);
            return Expr.Block(tempVariables, tempAssigns.Concat(realAssigns));
        }

        Expr Assign(VariableVisit variable, Expr value)
        {
            switch (variable.Type)
            {
                case VariableType.Identifier:
                    ParamExpr param;
                    if (scope.TryFindIdentifier(variable.Identifier, out param))
                        return Expr.Assign(param, value);

                    return Expr.Dynamic(context.BinderCache.GetSetMemberBinder(variable.Identifier),
                                        typeof(object), Expr.Constant(context.Globals), value);

                case VariableType.MemberId:
                    return Expr.Dynamic(context.BinderCache.GetSetMemberBinder(variable.Identifier),
                                        typeof(object), variable.Object, value);

                case VariableType.MemberExpr:
                    return Expr.Dynamic(context.BinderCache.GetSetIndexBinder(), typeof(object),
                                        variable.Object, variable.Member, value);

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        Expr IStatementVisitor<Expr>.Visit(Statement.Do statement)
        {
            scope = Scope.CreateChild(scope);
            return Visit(statement.Body);
        }

        Expr IStatementVisitor<Expr>.Visit(Statement.For statement)
        {
            // TODO: Check if number conversion failed by checking for double.NaN
            var convertBinder = context.BinderCache.GetConvertBinder(typeof(double));
            Func<Expression, Expr> toNumber = e => Expr.Dynamic(convertBinder, typeof(double), e.Visit(this));

            var parentScope = scope;
            scope = Scope.CreateChild(parentScope);

            var loopVariable = scope.AddLocal(statement.Identifier);
            var var = toNumber(statement.Var);
            var limit = toNumber(statement.Limit);
            var step = statement.Step == null
                           ? new Expression.Number(1.0).Visit(this)
                           : toNumber(statement.Step);

            var varVar = Expr.Variable(typeof(double));
            var limitVar = Expr.Variable(typeof(double));
            var stepVar = Expr.Variable(typeof(double));

            var breakConditionExpr =
                Expr.MakeBinary(
                    ExprType.OrElse,
                    Expr.MakeBinary(
                        ExprType.AndAlso,
                        Expr.GreaterThan(stepVar, Expr.Constant(0.0)),
                        Expr.GreaterThan(varVar, limitVar)),
                    Expr.MakeBinary(
                        ExprType.AndAlso,
                        Expr.LessThanOrEqual(stepVar, Expr.Constant(0.0)),
                        Expr.LessThan(varVar, limitVar)));

            var loopExpr =
                Expr.Loop(
                    Expr.Block(
                        Expr.IfThen(breakConditionExpr, Expr.Break(scope.BreakLabel())),
                        Expr.Assign(loopVariable, Expr.Convert(varVar, typeof(object))),
                        Visit(statement.Body),
                        Expr.AddAssign(varVar, stepVar)),
                    scope.BreakLabel());

            var expr =
                Expr.Block(
                    new[] {loopVariable, varVar, limitVar, stepVar},
                    Expr.Assign(varVar, var),
                    Expr.Assign(limitVar, limit),
                    Expr.Assign(stepVar, step),
                    loopExpr);

            scope = parentScope;
            return expr;
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
            return statement.Call.Visit(this);
        }

        Expr IStatementVisitor<Expr>.Visit(Statement.If statement)
        {
            throw new NotImplementedException();
        }

        Expr IStatementVisitor<Expr>.Visit(Statement.LocalAssign statement)
        {
            var locals = statement.Identifiers.Select(v => scope.AddLocal(v)).ToList();

            // Try to wrap all values except the last with varargs select
            var values = new List<Expr>(statement.Values.Count);
            for (int i = 0; i < values.Count - 1; i++)
                values.Add(TryWrapWithVarargsSelect(statement.Values[i]));
            values.Add(statement.Values.Last().Visit(this));

            if (statement.Values.Last().IsVarargsOrFuncCall())
                return VarargsExpandAssignment(locals, values);

            // Assign values to temporaries
            var tempVariables = values.Select(expr => Expr.Variable(expr.Type)).ToList();
            var tempAssigns = tempVariables.Zip(values, Expr.Assign);

            // Shrink or pad temporary's list with nil to match local's list length
            // and cast temporaries to locals type
            var tempVariablesResized = tempVariables
                .Resize(statement.Identifiers.Count, new Expression.Nil().Visit(this))
                .Zip(locals, (tempVar, local) => Expr.Convert(tempVar, local.Type));

            // Assign temporaries to locals
            var realAssigns = locals.Zip(tempVariablesResized, Expr.Assign);
            return Expr.Block(tempVariables, tempAssigns.Concat(realAssigns));
        }

        Expr TryWrapWithVarargsSelect(Expression expr)
        {
            // If expr is a varargs or function call expression we need return the first element in
            // the Varargs list if the value is of type Varargs or do nothing
            if (!expr.IsVarargsOrFuncCall())
                return expr.Visit(this);

            var variable = Expr.Variable(typeof(object));

            return
                Expr.Block(
                    new[] {variable},
                    Expr.Assign(variable, expr.Visit(this)),
                    Expr.IfThenElse(
                        Expr.TypeIs(variable, typeof(Varargs)),
                        Expr.Call(variable, typeof(Varargs).GetMethod("First")),
                        variable));
        }

        Expr VarargsExpandAssignment(List<ParamExpr> locals, List<Expr> values)
        {
            return Expr.Invoke(
                Expr.Constant((Action<IRuntimeVariables, object[]>)LuaOps.VarargsAssign),
                Expr.RuntimeVariables(locals),
                Expr.NewArrayInit(typeof(object), values));
        }

        Expr VarargsExpandAssignment(List<VariableVisit> variables, List<Expr> values)
        {
            var valuesVar = Expr.Variable(typeof(object[]));
            var invokeExpr =
                Expr.Invoke(
                    Expr.Constant((Func<int, object[], object[]>)LuaOps.VarargsAssign),
                    Expr.Constant(variables.Count),
                    Expr.NewArrayInit(typeof(object), values));
            var valuesAssign = Expr.Assign(valuesVar, invokeExpr);

            var varAssigns = variables
                .Select((var, i) => Assign(var, Expr.ArrayIndex(valuesVar, Expr.Constant(i))))
                .ToArray();

            var exprs = new Expr[varAssigns.Length + 1];
            exprs[0] = valuesAssign;
            Array.Copy(varAssigns, 0, exprs, 1, varAssigns.Length);

            return
                Expr.Block(
                    new[] {valuesVar},
                    exprs);
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
            var left = expression.Left.Visit(this);
            var right = expression.Right.Visit(this);
            ExprType operation;
            if (binaryExprTypes.TryGetValue(expression.Operation, out operation))
                return Expr.Dynamic(context.BinderCache.GetBinaryOperationBinder(operation),
                                    typeof(object), left, right);

            // BinaryOp have to be Concat at this point which can't be represented as a binary operation in the DLR
            return
                Expr.Call(
                    Expr.Field(
                        Expr.Constant(context),
                        typeof(Context).GetField("StringLibrary", BindingFlags.NonPublic | BindingFlags.Instance)),
                    typeof(LuaString).GetMethod("Concat", BindingFlags.NonPublic | BindingFlags.Instance),
                    Expr.Convert(left, typeof(object)), Expr.Convert(right, typeof(object)));
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
            return expression.Expression.Visit(this);
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
            var operand = expression.Operand.Visit(this);
            ExprType operation;
            if (unaryExprTypes.TryGetValue(expression.Operation, out operation))
                return Expr.Dynamic(context.BinderCache.GetUnaryOperationBinder(operation),
                                    typeof(object), operand);

            // UnaryOp have to be Length at this point which can't be represented as a unary operation in the DLR
            return Expr.Invoke(
                Expr.Constant((Func<Context, object, object>)LuaOps.Length),
                Expr.Constant(context),
                Expr.Convert(operand, typeof(object)));
        }

        Expr IExpressionVisitor<Expr>.Visit(Expression.Varargs expression)
        {
            throw new NotImplementedException();
        }

        VariableVisit IVariableVisitor<VariableVisit>.Visit(Variable.Identifier variable)
        {
            return VariableVisit.CreateIdentifier(variable.Value);
        }

        VariableVisit IVariableVisitor<VariableVisit>.Visit(Variable.MemberExpr variable)
        {
            return VariableVisit.CreateMemberExpr(
                variable.Prefix.Visit(this),
                variable.Member.Visit(this));
        }

        VariableVisit IVariableVisitor<VariableVisit>.Visit(Variable.MemberId variable)
        {
            return VariableVisit.CreateMemberId(
                variable.Prefix.Visit(this),
                variable.Member);
        }

        Expr IPrefixExpressionVisitor<Expr>.Visit(PrefixExpression.Expression prefixExpr)
        {
            return prefixExpr.Expr.Visit(this);
        }

        Expr IPrefixExpressionVisitor<Expr>.Visit(PrefixExpression.FunctionCall prefixExpr)
        {
            return prefixExpr.Call.Visit(this);
        }

        Expr IPrefixExpressionVisitor<Expr>.Visit(PrefixExpression.Variable prefixExpr)
        {
            var variable = prefixExpr.Var.Visit(this);
            switch (variable.Type)
            {
                case VariableType.Identifier:
                    ParamExpr param;
                    if (scope.TryFindIdentifier(variable.Identifier, out param))
                        return param;

                    return Expr.Dynamic(context.BinderCache.GetGetMemberBinder(variable.Identifier),
                                        typeof(object), Expr.Constant(context.Globals));

                case VariableType.MemberId:
                    return Expr.Dynamic(context.BinderCache.GetGetMemberBinder(variable.Identifier),
                                        typeof(object), variable.Object);

                case VariableType.MemberExpr:
                    return Expr.Dynamic(context.BinderCache.GetGetIndexBinder(), typeof(object),
                                        variable.Object, variable.Member);

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        Expr IFunctionCallVisitor<Expr>.Visit(FunctionCall.Normal functionCall)
        {
            var funcExpr = functionCall.Prefix.Visit(this);
            var argExprs = functionCall.Arguments.Visit(this);

            var invokeArgs = new Expr[argExprs.Length + 1];
            invokeArgs[0] = funcExpr;
            Array.Copy(argExprs, 0, invokeArgs, 1, argExprs.Length);

            return Expr.Dynamic(context.BinderCache.GetInvokeBinder(new CallInfo(argExprs.Length)),
                                typeof(object), invokeArgs);
        }

        Expr IFunctionCallVisitor<Expr>.Visit(FunctionCall.Table functionCall)
        {
            var tableExpr = functionCall.Prefix.Visit(this);
            var tableVar = Expr.Variable(typeof(object));
            var assignExpr = Expr.Assign(tableVar, tableExpr);

            var tableGetMember = Expr.Dynamic(context.BinderCache.GetGetMemberBinder(functionCall.Name),
                                              typeof(object), tableVar);

            var argExprs = functionCall.Arguments.Visit(this);
            var invokeArgs = new Expr[argExprs.Length + 2];
            invokeArgs[0] = tableGetMember;
            invokeArgs[1] = tableVar;
            Array.Copy(argExprs, 0, invokeArgs, 2, argExprs.Length);

            var invokeExpr = Expr.Dynamic(context.BinderCache.GetInvokeBinder(new CallInfo(argExprs.Length)),
                                          typeof(object), invokeArgs);

            return
                Expr.Block(
                    new[] {tableVar},
                    assignExpr,
                    invokeExpr);
        }

        Expr[] IArgumentsVisitor<Expr[]>.Visit(Arguments.Normal arguments)
        {
            return arguments.Arguments.Select(e => e.Visit(this)).ToArray();
        }

        Expr[] IArgumentsVisitor<Expr[]>.Visit(Arguments.String arguments)
        {
            return new[] {arguments.Literal.Visit(this)};
        }

        Expr[] IArgumentsVisitor<Expr[]>.Visit(Arguments.Table arguments)
        {
            return new[] {arguments.Value.Visit(this)};
        }
    }
}
