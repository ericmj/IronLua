using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using IronLua.Compiler.Ast;
using IronLua.Runtime;
using IronLua.Util;
using Expr = System.Linq.Expressions.Expression;
using ParamExpr = System.Linq.Expressions.ParameterExpression;
using ExprType = System.Linq.Expressions.ExpressionType;
using Expression = IronLua.Compiler.Ast.Expression;

namespace IronLua.Compiler
{
    class Generator : IStatementVisitor<Expr>, ILastStatementVisitor<Expr>, IExpressionVisitor<Expr>,
                      IVariableVisitor<VariableVisit>, IPrefixExpressionVisitor<Expr>, IFunctionCallVisitor<Expr>,
                      IArgumentsVisitor<Expr[]>, IFieldVisitor<FieldVisit>
    {
        static readonly Dictionary<BinaryOp, ExprType> binaryExprTypes =
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

        static readonly Dictionary<UnaryOp, ExprType> unaryExprTypes =
            new Dictionary<UnaryOp, ExprType>
                {
                    {UnaryOp.Negate, ExprType.Negate},
                    {UnaryOp.Not,    ExprType.Not}
                };

        Scope scope;
        readonly Context context;

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
            var locals = scope.AllLocals();

            // Don't output blocks if we don't declare any locals and it's a single statement
            var expr = locals.Length == 0 && statementExprs.Count == 1
                           ? statementExprs[0]
                           : Expr.Block(locals, statementExprs);

            scope = parentScope;
            return expr;
        }

        Expr Visit(FunctionBody function)
        {
            var parentScope = scope;
            scope = Scope.CreateFunctionChild(scope);
            var returnLabel = scope.AddReturnLabel();

            var parameters = function.Parameters.Select(p => scope.AddLocal(p)).ToList();
            if (function.Varargs)
                parameters.Add(scope.AddLocal(Constant.VARARGS, typeof(Varargs)));

            var bodyExpr = Expr.Block(Visit(function.Body), Expr.Label(returnLabel, Expr.Constant(null)));
            var lambdaExpr = Expr.Lambda(bodyExpr, parameters);

            scope = parentScope;
            return lambdaExpr;
        }

        Expr IStatementVisitor<Expr>.Visit(Statement.Assign statement)
        {
            var variables = statement.Variables.Select(v => v.Visit(this)).ToList();
            var values = WrapWithVarargsFirst(statement.Values);

            if (statement.Values.Last().IsVarargs() || statement.Values.Last().IsFunctionCall())
                return VarargsExpandAssignment(variables, values);

            return AssignWithTemporaries(variables, values, Assign);
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
            var iterFuncVar = Expr.Variable(typeof(object));
            var iterStateVar = Expr.Variable(typeof(object));
            var iterableVar = Expr.Variable(typeof(object));
            var iterVars = new[] {iterFuncVar, iterStateVar, iterableVar};

            var valueExprs = statement.Values.Select(v => Expr.Convert(v.Visit(this), typeof(object)));
            var assignIterVars = VarargsExpandAssignment(iterVars, valueExprs);

            var parentScope = scope;
            scope = Scope.CreateChild(scope);
            var locals = statement.Identifiers.Select(id => scope.AddLocal(id)).ToList();

            var invokeIterFunc = Expr.Dynamic(context.BinderCache.GetInvokeBinder(new CallInfo(2)),
                                              typeof(object), iterFuncVar, iterStateVar, iterableVar);
            var loop =
                Expr.Loop(
                    Expr.Block(
                        locals,
                        VarargsExpandAssignment(
                            locals,
                            new[] {invokeIterFunc}),
                        Expr.IfThen(Expr.Equal(locals[0], Expr.Constant(null)), Expr.Break(scope.BreakLabel())),
                        Visit(statement.Body)),
                    scope.BreakLabel());

            var expr = Expr.Block(iterVars, assignIterVars, loop);

            scope = parentScope;
            return expr;
        }

        Expr IStatementVisitor<Expr>.Visit(Statement.Function statement)
        {
            // Rewrite AST to its desugared state
            // function a:b (params) body end -> function a.b (self, params) body end
            if (statement.Name.Table != null)
            {
                statement.Body.Parameters.Insert(0, "self");
                statement.Name.Identifiers.Add(statement.Name.Table);
                statement.Name.Table = null;
            }

            var bodyExpr = Visit(statement.Body);
            return AssignToIdentifierList(statement.Name.Identifiers, bodyExpr);
        }

        Expr IStatementVisitor<Expr>.Visit(Statement.FunctionCall statement)
        {
            return statement.Call.Visit(this);
        }

        Expr IStatementVisitor<Expr>.Visit(Statement.If statement)
        {
            var testExpr = statement.Test.Visit(this);
            var bodyExpr = Visit(statement.Body);
            var elseExpr = statement.ElseBody != null ? Visit(statement.ElseBody) : Expr.Default(typeof(void));
            var elseifExprs = statement.Elseifs.Aggregate(elseExpr, ElseifCombiner);

            return
                Expr.IfThenElse(
                    Expr.Dynamic(
                        context.BinderCache.GetConvertBinder(typeof(bool)),
                        typeof(bool),
                        testExpr),
                    bodyExpr,
                    elseifExprs);
        }

        Expr ElseifCombiner(Expr expr, Elseif elseif)
        {
            return
                Expr.IfThenElse(Expr.Dynamic(
                        context.BinderCache.GetConvertBinder(typeof(bool)),
                        typeof(bool),
                        elseif.Test.Visit(this)),
                    Visit(elseif.Body),
                    expr);
        }

        Expr IStatementVisitor<Expr>.Visit(Statement.LocalAssign statement)
        {
            var locals = statement.Identifiers.Select(v => scope.AddLocal(v)).ToList();
            var values = WrapWithVarargsFirst(statement.Values);

            if (statement.Values.Last().IsVarargs() || statement.Values.Last().IsFunctionCall())
                return VarargsExpandAssignment(locals, values);

            return AssignWithTemporaries(locals, values, Expr.Assign);
        }

        Expr IStatementVisitor<Expr>.Visit(Statement.LocalFunction statement)
        {
            return Expr.Assign(scope.AddLocal(statement.Identifier), Visit(statement.Body));
        }

        Expr IStatementVisitor<Expr>.Visit(Statement.Repeat statement)
        {
            // Temporarily rewrite the AST so that the test expression can be evaulated in the same scope as the body
            statement.Body.Statements.Add(
                new Statement.If(
                    statement.Test,
                    new Block(new List<Statement>(), new LastStatement.Break()),
                    new List<Elseif>(),
                    null));

            var breakLabel = scope.BreakLabel();
            var expr = Expr.Loop(
                Visit(statement.Body),
                breakLabel);

            statement.Body.Statements.RemoveAt(statement.Body.Statements.Count - 2);
            return expr;
        }

        Expr IStatementVisitor<Expr>.Visit(Statement.While statement)
        {
            return Expr.Loop(
                Expr.IfThenElse(
                    Expr.Dynamic(
                        context.BinderCache.GetConvertBinder(typeof(bool)),
                        typeof(bool),
                        statement.Test.Visit(this)),
                    Visit(statement.Body),
                    Expr.Break(scope.BreakLabel())),
                scope.BreakLabel());
        }

        Expr ILastStatementVisitor<Expr>.Visit(LastStatement.Break lastStatement)
        {
            return Expr.Break(scope.BreakLabel());
        }

        Expr ILastStatementVisitor<Expr>.Visit(LastStatement.Return lastStatement)
        {
            var returnLabel = scope.GetReturnLabel();

            if (returnLabel == null)
                return Expr.Empty();

            var returnValues = lastStatement.Values
                .Select(expr => Expr.Convert(expr.Visit(this), typeof(object))).ToArray();

            if (returnValues.Length == 0)
                return Expr.Return(returnLabel);
            if (returnValues.Length == 1)
                return Expr.Return(returnLabel, returnValues[0]);

            return Expr.Return(
                returnLabel,
                Expr.New(Methods.NewVarargs, Expr.NewArrayInit(typeof(object), returnValues)));
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
                Expr.Invoke(
                    Expr.Constant((Func<Context, object, object, object>)LuaOps.Concat),
                    Expr.Constant(context),
                    Expr.Convert(left, typeof(object)),
                    Expr.Convert(right, typeof(object)));
        }

        Expr IExpressionVisitor<Expr>.Visit(Expression.Boolean expression)
        {
            return Expr.Constant(expression.Literal);
        }

        Expr IExpressionVisitor<Expr>.Visit(Expression.Function expression)
        {
            return Visit(expression.Body);
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
            var newTableExpr = Expr.New(Methods.NewLuaTable);
            var tableVar = Expr.Variable(typeof(LuaTable));
            var tableAssign = Expr.Assign(tableVar, newTableExpr);

            int intIndex = 1;
            var fieldInitsExprs = expression.Fields
                .Select(f => TableSetValue(tableVar, f.Visit(this), ref intIndex))
                .ToArray();

            var exprs = new Expr[fieldInitsExprs.Length + 2];
            exprs[0] = tableAssign;
            exprs[exprs.Length - 1] = tableVar;
            Array.Copy(fieldInitsExprs, 0, exprs, 1, fieldInitsExprs.Length);

            return Expr.Block(new [] {tableVar}, exprs);
        }

        Expr TableSetValue(Expr table, FieldVisit field, ref int intIndex)
        {
            switch (field.Type)
            {
                case FieldVisitType.Implicit:
                    return Expr.Call(table, Methods.LuaTableSetValue,
                                     Expr.Constant(intIndex++, typeof(object)),
                                     Expr.Convert(field.Value, typeof(object)));
                case FieldVisitType.Explicit:
                    return Expr.Call(table, Methods.LuaTableSetValue,
                                     Expr.Convert(field.Member, typeof(object)),
                                     Expr.Convert(field.Value, typeof(object)));
                default:
                    throw new ArgumentOutOfRangeException();
            }
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
            ParamExpr param;
            if (scope.TryGetLocal(Constant.VARARGS, out param))
                return param;
            return Expr.Constant(null);
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
                    ParamExpr local;
                    if (scope.TryGetLocal(variable.Identifier, out local))
                        return local;

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

        FieldVisit IFieldVisitor<FieldVisit>.Visit(Field.MemberExpr field)
        {
            return FieldVisit.CreateExplicit(field.Member.Visit(this), field.Value.Visit(this));
        }

        FieldVisit IFieldVisitor<FieldVisit>.Visit(Field.MemberId field)
        {
            return FieldVisit.CreateExplicit(Expr.Constant(field.Member), field.Value.Visit(this));
        }

        FieldVisit IFieldVisitor<FieldVisit>.Visit(Field.Normal field)
        {
            return FieldVisit.CreateImplicit(field.Value.Visit(this));
        }

        Expr AssignWithTemporaries<T>(List<T> variables, List<Expr> values, Func<T, Expr, Expr> assigner)
        {
            // Assign values to temporaries
            var tempVariables = values.Select(expr => Expr.Variable(expr.Type)).ToList();
            var tempAssigns = tempVariables.Zip(values, Expr.Assign);

            // Shrink or pad temporary's list with nil to match variables's list length
            // and cast temporaries to object type
            var tempVariablesResized = tempVariables
                .Resize(variables.Count, new Expression.Nil().Visit(this))
                .Select(tempVar => Expr.Convert(tempVar, typeof(object)));

            // Assign temporaries to globals
            var realAssigns = variables.Zip(tempVariablesResized, assigner);
            return Expr.Block(tempVariables, tempAssigns.Concat(realAssigns));
        }

        Expr Assign(VariableVisit variable, Expr value)
        {
            switch (variable.Type)
            {
                case VariableType.Identifier:
                    ParamExpr local;
                    if (scope.TryGetLocal(variable.Identifier, out local))
                        return Expr.Assign(local, value);

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

        Expr AssignToIdentifierList(List<string> identifiers, Expr value)
        {
            Expr expr;
            var firstId = identifiers.First();

            ParamExpr local;
            bool isLocal = scope.TryGetLocal(firstId, out local);

            // If there is just a single identifier return the assignment to it
            if (identifiers.Count == 1)
            {
                if (isLocal)
                    return Expr.Assign(local, value);
                return Expr.Dynamic(context.BinderCache.GetSetMemberBinder(firstId),
                                    typeof(object), Expr.Constant(context.Globals), value);
            }

            // First element can be either a local or global variable
            if (isLocal)
                expr = local;
            else
                expr = Expr.Dynamic(context.BinderCache.GetGetMemberBinder(firstId),
                                    typeof(object), Expr.Constant(context.Globals));

            // Loop over all elements except the first and the last and perform get member on them
            expr = identifiers
                .Skip(1).Take(identifiers.Count - 2)
                .Aggregate(expr, (e, id) => Expr.Dynamic(context.BinderCache.GetGetMemberBinder(id), typeof(object), e));

            // Do the assignment on the last identifier
            return Expr.Dynamic(context.BinderCache.GetSetMemberBinder(identifiers.Last()),
                                        typeof(object), expr, value);
        }
        List<Expr> WrapWithVarargsFirst(List<Expression> values)
        {
            // Try to wrap all values except the last with varargs select
            return values
                .Take(values.Count - 1)
                .Select(TryWrapWithVarargsFirst)
                .Add(values.Last().Visit(this))
                .ToList();
        }

        Expr TryWrapWithVarargsFirst(Expression value)
        {
            var valueExpr = Expr.Convert(value.Visit(this), typeof(object));

            // If expr is a varargs or function call expression we need to return the first element in
            // the Varargs list if the value is of type Varargs or do nothing
            if (value.IsVarargs())
                return Expr.Call(valueExpr, Methods.VarargsFirst);

            if (value.IsFunctionCall())
            {
                var variable = Expr.Variable(typeof(object));

                return
                    Expr.Block(
                        new[] { variable },
                        Expr.Assign(variable, valueExpr),
                        Expr.IfThenElse(
                            Expr.TypeIs(variable, typeof(Varargs)),
                            Expr.Call(variable, Methods.VarargsFirst),
                            variable));
            }

            return valueExpr;
        }

        Expr VarargsExpandAssignment(IEnumerable<ParameterExpression> locals, IEnumerable<Expr> values)
        {
            return Expr.Invoke(
                Expr.Constant((Action<IRuntimeVariables, object[]>)LuaOps.VarargsAssign),
                Expr.RuntimeVariables(locals),
                Expr.NewArrayInit(
                    typeof(object),
                    values.Select(value => Expr.Convert(value, typeof(object)))));
        }

        Expr VarargsExpandAssignment(List<VariableVisit> variables, IEnumerable<Expr> values)
        {
            var valuesVar = Expr.Variable(typeof(object[]));
            var invokeExpr =
                Expr.Invoke(
                    Expr.Constant((Func<int, object[], object[]>)LuaOps.VarargsAssign),
                    Expr.Constant(variables.Count),
                    Expr.NewArrayInit(
                        typeof(object),
                        values.Select(value => Expr.Convert(value, typeof(object)))));
            var valuesAssign = Expr.Assign(valuesVar, invokeExpr);

            var varAssigns = variables
                .Select((var, i) => Assign(var, Expr.ArrayIndex(valuesVar, Expr.Constant(i))))
                .ToArray();

            var exprs = new Expr[varAssigns.Length + 1];
            exprs[0] = valuesAssign;
            Array.Copy(varAssigns, 0, exprs, 1, varAssigns.Length);

            return
                Expr.Block(
                    new[] { valuesVar },
                    exprs);
        }
    }
}
