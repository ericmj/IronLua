using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using IronLua.Compiler.Ast;
using IronLua.Runtime;
using IronLua.Util;
using Microsoft.Scripting;
using Microsoft.Scripting.Runtime;
using Microsoft.Scripting.Utils;
using Expr = System.Linq.Expressions.Expression;
using ParamExpr = System.Linq.Expressions.ParameterExpression;
using ExprType = System.Linq.Expressions.ExpressionType;
using Expression = IronLua.Compiler.Ast.Expression;

namespace IronLua.Compiler
{
    class Generator : IStatementVisitor<Expr>, IExpressionVisitor<Expr>,
                      IVariableVisitor<VariableVisit>, IPrefixExpressionVisitor<Expr>, IFunctionCallVisitor<Expr>,
                      IArgumentsVisitor<Expr[]>, IFieldVisitor<FieldVisit>
    {
        static readonly Dictionary<BinaryOp, ExprType> binaryExprTypes =
            new Dictionary<BinaryOp, ExprType>
                {
                    {BinaryOp.Or,           ExprType.Or/*Else*/},
                    {BinaryOp.And,          ExprType.And/*Also*/},
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

        LuaScope scope;
        readonly LuaContext context;
        SymbolDocumentInfo _document;

        public Generator(LuaContext context)
        {
            ContractUtils.RequiresNotNull(context, "context");
            this.context = context;
        }

        public Expression<Func<IDynamicMetaObjectProvider, dynamic>> Compile(Block block, SourceUnit sourceUnit = null)
        {
            if (sourceUnit != null)
                _document = sourceUnit.Document ?? Expr.SymbolDocument("(chunk)", sourceUnit.LanguageContext.LanguageGuid, sourceUnit.LanguageContext.VendorGuid);

            var dlrGlobals = Expr.Parameter(typeof(IDynamicMetaObjectProvider), "_DLR");
            scope = LuaScope.CreateRoot(dlrGlobals);
            var blockExpr = Visit(block);
            var expr = Expr.Block(blockExpr, Expr.Label(scope.GetReturnLabel(), Expr.Constant(null)));
            return Expr.Lambda<Func<IDynamicMetaObjectProvider, dynamic>>(expr, dlrGlobals);
        }

        Expr Visit(Block block)
        {
            var parentScope = scope;
            try
            {
                scope = LuaScope.CreateChildFrom(parentScope);

                var statementExprs = new List<Expr>();
                if (block.Statements.Count > 0)
                {
                    if (_document != null)
                    {
                        statementExprs.Add(Expr.DebugInfo(_document, 
                            block.Span.Start.Line, block.Span.Start.Column, 
                            block.Span.End.Line, block.Span.End.Column));
                    }
                    statementExprs.AddRange(block.Statements.Select(s => s.Visit(this)));
                }

                if (statementExprs.Count == 0)
                    return Expr.Empty();
                else if (statementExprs.Count == 1 && scope.LocalsCount == 0)
                    // Don't output blocks if we don't declare any locals and it's a single statement
                    return statementExprs[0];
                else
                    return Expr.Block(scope.GetLocals(), statementExprs);
            } 
            finally
            {
                scope = parentScope;
            }
        }

        Expr Visit(FunctionName name, FunctionBody function)
        {
            var parentScope = scope;
            try
            {
                scope = LuaScope.CreateFunctionChildFrom(scope);

                var parameters = new List<ParamExpr>();
                if (name.HasTableMethod)
                    parameters.Add(scope.AddLocal("self"));
                parameters.AddRange(
                    function.Parameters.Select(p => scope.AddLocal(p)));
                if (function.HasVarargs)
                    parameters.Add(scope.AddLocal(Constant.VARARGS, typeof(Varargs)));

                var bodyExpr = Expr.Block(Visit(function.Body),
                                Expr.Label(scope.GetReturnLabel(), Expr.Constant(null)));

                var funcName = Constant.FUNCTION_PREFIX + name.Identifiers.Last();
                return Expr.Lambda(bodyExpr, funcName, parameters);
            }
            finally
            {
                scope = parentScope;
            }
        }

        Expr IStatementVisitor<Expr>.Visit(Statement.Assign statement)
        {
            var variables = statement.Variables.Select(v => v.Visit(this)).ToList();
            var values = WrapWithVarargsFirst(statement.Values);

            var lastValue = statement.Values.Last();
            if (lastValue.IsVarargs() || lastValue.IsFunctionCall())
                return VarargsExpandAssignment(variables, values);

            return AssignWithTemporaries(variables, values, Assign);
        }

        Expr IStatementVisitor<Expr>.Visit(Statement.Do statement)
        {
            scope = LuaScope.CreateChildFrom(scope);
            return Visit(statement.Body);
        }

        Expr IStatementVisitor<Expr>.Visit(Statement.For statement)
        {
            var parentScope = scope;
            try
            {
                scope = LuaScope.CreateChildFrom(parentScope);

                var step = statement.Step == null
                               ? new Expression.Number(1.0).Visit(this)
                               : ExprHelpers.ConvertToNumber(context, statement.Step.Visit(this));

                var loopVariable = scope.AddLocal(statement.Identifier);
                var varVar = Expr.Variable(typeof(double));
                var limitVar = Expr.Variable(typeof(double));
                var stepVar = Expr.Variable(typeof(double));

                var breakConditionExpr = ForLoopBreakCondition(limitVar, stepVar, varVar);

                return Expr.Block(
                    new[] { loopVariable, varVar, limitVar, stepVar },
                    Expr.Assign(varVar, ExprHelpers.ConvertToNumber(context, statement.Var.Visit(this))),
                    Expr.Assign(limitVar, ExprHelpers.ConvertToNumber(context, statement.Limit.Visit(this))),
                    Expr.Assign(stepVar, step),
                    ExprHelpers.CheckNumberForNan(varVar, String.Format(ExceptionMessage.FOR_VALUE_NOT_NUMBER, "inital value")),
                    ExprHelpers.CheckNumberForNan(limitVar, String.Format(ExceptionMessage.FOR_VALUE_NOT_NUMBER, "limit")),
                    ExprHelpers.CheckNumberForNan(stepVar, String.Format(ExceptionMessage.FOR_VALUE_NOT_NUMBER, "step")),
                    ForLoop(statement, stepVar, loopVariable, varVar, breakConditionExpr));
            }
            finally
            {
                scope = parentScope;
            }
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
            scope = LuaScope.CreateChildFrom(scope);
            var locals = statement.Identifiers.Select(id => scope.AddLocal(id)).ToList();

            var invokeIterFunc = Expr.Dynamic(context.CreateInvokeBinder(new CallInfo(2)),
                                              typeof(object), iterFuncVar, iterStateVar, iterableVar);
            var loop =
                Expr.Loop(
                    Expr.Block(
                        locals,
                        VarargsExpandAssignment(
                            locals,
                            new[] {invokeIterFunc}),
                        Expr.IfThen(Expr.Equal(locals[0], Expr.Constant(null)), Expr.Break(scope.BreakLabel())),
                        Expr.Assign(iterableVar, locals[0]),
                        Visit(statement.Body)),
                    scope.BreakLabel());

            var expr = Expr.Block(iterVars, assignIterVars, loop);

            scope = parentScope;
            return expr;
        }

        Expr IStatementVisitor<Expr>.Visit(Statement.Function statement)
        {
            var bodyExpr = Visit(statement.Name, statement.Body);

            if (statement.IsLocal)
            {
                var localExpr = scope.AddLocal(statement.Name.Identifiers.Last());
                return Expr.Assign(localExpr, bodyExpr);
            }

            return AssignToIdentifierList(statement.Name.Identifiers, bodyExpr);
        }

        Expr IStatementVisitor<Expr>.Visit(Statement.FunctionCall statement)
        {
            return statement.Call.Visit(this);
        }

        Expr IStatementVisitor<Expr>.Visit(Statement.If statement)
        {
            var binder = context.CreateConvertBinder(typeof(bool), false);
            var expr = statement.ElseBody != null 
                     ? Visit(statement.ElseBody)
                     : Expr.Empty();

            var list = statement.IfList;
            for (int i = list.Count - 1; i >= 0; --i)
            {
                var ifThen = list[i];
                expr = Expr.IfThenElse(
                         Expr.Dynamic(binder, typeof(bool), ifThen.Test.Visit(this)),
                         Visit(ifThen.Body),
                         expr);
            }

            return expr;
        }

        Expr IStatementVisitor<Expr>.Visit(Statement.LocalAssign statement)
        {
            var values = (statement.Values != null && statement.Values.Count > 0) 
                       ? WrapWithVarargsFirst(statement.Values) : new List<Expr>();
            var locals = statement.Identifiers.Select(v => scope.AddLocal(v)).ToList();

            if (statement.Values != null && statement.Values.Count > 0)
            {
                var lastValue = statement.Values.Last();
                if (lastValue.IsVarargs() || lastValue.IsFunctionCall())
                    return VarargsExpandAssignment(locals, values);
            }

            return AssignWithTemporaries(locals, values, Expr.Assign);
        }

        Expr IStatementVisitor<Expr>.Visit(Statement.LocalFunction statement)
        {
            var bodyExpr = Visit(statement.Name, statement.Body);
            var localExpr = scope.AddLocal(statement.Name.Identifiers.Last());
            return Expr.Assign(localExpr, bodyExpr);
        }

        Expr IStatementVisitor<Expr>.Visit(Statement.Repeat statement)
        {
            var stats = statement.Body.Statements;

            // Temporarily rewrite the AST so that the test expression 
            // can be evaluated in the same scope as the body.
            stats.Add(new Statement.If(statement.Test, 
                new Block(new LastStatement.Break() { Span = statement.Test.Span }))
            {
                Span = statement.Test.Span
            });

            var breakLabel = scope.BreakLabel();
            var expr = Expr.Loop(
                Visit(statement.Body),
                breakLabel);

            // Remove the temporary statement we added.
            stats.RemoveAt(stats.Count - 1);
            return expr;
        }

        Expr IStatementVisitor<Expr>.Visit(Statement.While statement)
        {
            var breakLabel = scope.BreakLabel();
            var stat = Expr.Loop(
                Expr.IfThenElse(
                    Expr.Dynamic(
                        context.CreateConvertBinder(typeof(bool), false),
                        typeof(bool),
                        statement.Test.Visit(this)),
                    Visit(statement.Body),
                    Expr.Break(breakLabel)),
                breakLabel);
            return stat;
        }

        Expr IStatementVisitor<Expr>.Visit(Statement.Goto statement)
        {
            ContractUtils.RequiresNotNull(statement.LabelName, "LabelName");

            if (statement.LabelName == "@break")
            {
                return Expr.Break(scope.BreakLabel());
            }

            return Expr.Goto(scope.AddLabel(statement.LabelName));
        }

        Expr IStatementVisitor<Expr>.Visit(Statement.LabelDecl statement)
        {
            return Expr.Label(scope.AddLabel(statement.LabelName));
        }

        Expr IStatementVisitor<Expr>.Visit(LastStatement.Break statement)
        {
            return Expr.Break(scope.BreakLabel());
        }

        Expr IStatementVisitor<Expr>.Visit(LastStatement.Return statement)
        {
            var returnLabel = scope.GetReturnLabel();

            if (returnLabel == null)
                return Expr.Empty();

            var returnValues = statement.Values
                .Select(expr => Expr.Convert(expr.Visit(this), typeof(object))).ToArray();

            if (returnValues.Length == 0)
                return Expr.Return(returnLabel, Expr.Constant(null)); // hack!
                //return Expr.Return(returnLabel); // FIXME: how do we get the return label to be void in this case?
            if (returnValues.Length == 1)
                return Expr.Return(returnLabel, returnValues[0]);

            return Expr.Return(
                returnLabel,
                Expr.New(MemberInfos.NewVarargs, Expr.NewArrayInit(typeof(object), returnValues)));
        }

        Expr IExpressionVisitor<Expr>.Visit(Expression.BinaryOp expression)
        {
            var left = expression.Left.Visit(this);
            var right = expression.Right.Visit(this);
            ExprType operation;
            if (binaryExprTypes.TryGetValue(expression.Operation, out operation))
                return Expr.Dynamic(context.CreateBinaryOperationBinder(operation),
                                    typeof(object), left, right);

            // BinaryOp have to be Concat at this point which can't be represented as a binary operation in the DLR
            return
                Expr.Invoke(
                    Expr.Constant((Func<LuaContext, object, object, object>)LuaOps.Concat),
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
            return Visit(new FunctionName("lambda"), expression.Body);
        }

        Expr IExpressionVisitor<Expr>.Visit(Expression.Nil expression)
        {
            return Expr.Constant(null, typeof(DynamicNull));
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
            var newTableExpr = Expr.New(MemberInfos.NewLuaTable);
            var tableVar = Expr.Variable(typeof(LuaTable));
            var tableAssign = Expr.Assign(tableVar, newTableExpr);

            double intIndex = 1;
            var fieldInitsExprs = expression.Fields
                .Select(f => TableSetValue(tableVar, f.Visit(this), ref intIndex))
                .ToArray();

            var exprs = new Expr[fieldInitsExprs.Length + 2];
            exprs[0] = tableAssign;
            exprs[exprs.Length - 1] = tableVar;
            Array.Copy(fieldInitsExprs, 0, exprs, 1, fieldInitsExprs.Length);

            return Expr.Block(new [] {tableVar}, exprs);
        }

        Expr TableSetValue(Expr table, FieldVisit field, ref double intIndex)
        {
            switch (field.Type)
            {
                case FieldVisitType.Implicit:
                    return Expr.Call(table, MemberInfos.LuaTableSetValue,
                                     Expr.Constant(intIndex++, typeof(object)),
                                     Expr.Convert(field.Value, typeof(object)));
                case FieldVisitType.Explicit:
                    return Expr.Call(table, MemberInfos.LuaTableSetValue,
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
                return Expr.Dynamic(context.CreateUnaryOperationBinder(operation),
                                    typeof(object), operand);

            // UnaryOp have to be Length at this point which can't be represented as a unary operation in the DLR
            return Expr.Invoke(
                Expr.Constant((Func<LuaContext, object, object>)LuaOps.Length),
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


        Expr CreateGlobalGetMember(string identifier, LuaTable globals, LuaScope scope)
        {
            var temp = Expr.Parameter(typeof(object));

            if (globals.HasValue(identifier))
                return Expr.Block(
                    typeof(object),
                    scope.AllLocals().Add(temp),
                    Expr.Assign(temp, Expr.Constant(null)),
                    Expr.TryCatch(Expr.Assign(temp, Expr.Dynamic(context.CreateGetMemberBinder(identifier, false),
                                    typeof(object), Expr.Constant(globals))),
                                    Expr.Catch(Expr.Parameter(typeof(Exception)), Expr.Constant(null))),
                    temp);


            return Expr.Block(
                    typeof(object),
                    scope.AllLocals().Add(temp),
                    Expr.Assign(temp, Expr.Constant(null)),
                    Expr.TryCatch(Expr.Assign(temp, Expr.Dynamic(context.CreateGetMemberBinder(identifier, false),
                                    typeof(object), scope.GetDlrGlobals())),
                                    Expr.Catch(Expr.Parameter(typeof(Exception)), Expr.Constant(null))),
                    temp);
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

                    return CreateGlobalGetMember(variable.Identifier, context.Globals, scope);
                    
                    //return Expr.Dynamic(context.CreateGetMemberBinder(variable.Identifier, false),
                    //                    typeof(object), Expr.Constant(context.Globals));

                    //return Expr.Dynamic(context.CreateGetMemberBinder(variable.Identifier, false),
                    //                    typeof(object), scope.GetDlrGlobals());

                case VariableType.MemberId:
                    return Expr.Dynamic(context.CreateGetMemberBinder(variable.Identifier, false),
                                        typeof(object), variable.Object);

                case VariableType.MemberExpr:
                    return Expr.Dynamic(context.CreateGetIndexBinder(new CallInfo(1)),
                                        typeof(object), variable.Object, variable.Member);

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

            return Expr.Dynamic(context.CreateInvokeBinder(new CallInfo(argExprs.Length)),
                                typeof(object), invokeArgs);
        }

        Expr IFunctionCallVisitor<Expr>.Visit(FunctionCall.Table functionCall)
        {
            var tableExpr = functionCall.Prefix.Visit(this);
            var tableVar = Expr.Variable(typeof(object));
            var assignExpr = Expr.Assign(tableVar, tableExpr);

            var tableGetMember = Expr.Dynamic(context.CreateGetMemberBinder(functionCall.MethodName, false),
                                              typeof(object), tableVar);

            var argExprs = functionCall.Arguments.Visit(this);
            var invokeArgs = new Expr[argExprs.Length + 2];
            invokeArgs[0] = tableGetMember;
            invokeArgs[1] = tableVar;
            Array.Copy(argExprs, 0, invokeArgs, 2, argExprs.Length);

            var invokeExpr = Expr.Dynamic(context.CreateInvokeBinder(new CallInfo(argExprs.Length)),
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

        Expr CreateGlobalSetMember(string identifier, Expr globals, LuaScope scope, Expr value)
        {
            var scopeAssign = Expr.Dynamic(context.CreateSetMemberBinder(identifier, false),
                                    typeof(object), scope.GetDlrGlobals(), value);

            var scopeDelete = Expr.TryCatch(Expr.Dynamic(context.CreateDeleteMemberBinder(identifier, false),
                                    typeof(void), scope.GetDlrGlobals()), Expr.Catch(Expr.Parameter(typeof(Exception)), Expr.Empty()));

            return Expr.Condition(Expr.Equal(value, Expr.Constant(null)), Expr.Block(scopeDelete, Expr.Constant(null)), scopeAssign);
        }

        Expr Assign(VariableVisit variable, Expr value)
        {
            switch (variable.Type)
            {
                case VariableType.Identifier:
                    ParamExpr local;
                    if (scope.TryGetLocal(variable.Identifier, out local))
                        return Expr.Assign(local, value);


                    return CreateGlobalSetMember(variable.Identifier, Expr.Constant(context.Globals), scope, value);

                    //return Expr.Dynamic(context.CreateSetMemberBinder(variable.Identifier, false),
                    //                    typeof(object), Expr.Constant(context.Globals), value);

                    //return Expr.Dynamic(context.CreateSetMemberBinder(variable.Identifier, false),
                    //                    typeof(object), scope.GetDlrGlobals(), value);

                case VariableType.MemberId:
                    return Expr.Dynamic(context.CreateSetMemberBinder(variable.Identifier, false),
                                        typeof(object), variable.Object, value);

                case VariableType.MemberExpr:
                    return Expr.Dynamic(context.CreateSetIndexBinder(new CallInfo(1)),
                                        typeof(object), variable.Object, variable.Member, value);

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

                return CreateGlobalSetMember(firstId, Expr.Constant(context.Globals), scope, value);
                //return Expr.Dynamic(context.CreateSetMemberBinder(firstId, false),
                //                            typeof(object),
                //                            Expr.Constant(context.Globals),
                //                            value);
            }

            // First element can be either a local or global variable
            if (isLocal)
                expr = local;
            else
                expr = CreateGlobalGetMember(firstId, context.Globals, scope);
                    //Expr.Dynamic(context.CreateGetMemberBinder(firstId, false),
                    //                        typeof(object),
                    //                        Expr.Constant(context.Globals));

            // Loop over all elements except the first and the last and perform get member on them
            expr = identifiers
                .Skip(1).Take(identifiers.Count - 2)
                .Aggregate(expr, (e, id) =>
                    Expr.Dynamic(context.CreateGetMemberBinder(id, false),
                                         typeof (object), e));

            // Do the assignment on the last identifier
            return Expr.Dynamic(context.CreateSetMemberBinder(identifiers.Last(), false),
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
            // If expr is a varargs or function call expression we need to return the first element in
            // the Varargs list if the value is of type Varargs or do nothing
            if (value.IsVarargs())
            {
                var varargsExpr = Expr.Convert(value.Visit(this), typeof(Varargs));

                return Expr.Call(varargsExpr, MemberInfos.VarargsFirst);
            }

            var valueExpr = Expr.Convert(value.Visit(this), typeof(object));

            if (value.IsFunctionCall())
            {
                var variable = Expr.Variable(typeof(object));

                return
                    Expr.Block(
                        new[] { variable },
                        Expr.Assign(variable, valueExpr),
                        Expr.Condition(
                            Expr.TypeIs(variable, typeof(Varargs)),
                            Expr.Call(Expr.Convert(variable, typeof(Varargs)), MemberInfos.VarargsFirst),
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

        LoopExpression ForLoop(Statement.For statement, ParameterExpression stepVar, ParameterExpression loopVariable,
                               ParameterExpression varVar, BinaryExpression breakConditionExpr)
        {
            var loopExpr =
                Expr.Loop(
                    Expr.Block(
                        Expr.IfThen(breakConditionExpr, Expr.Break(scope.BreakLabel())),
                        Expr.Assign(loopVariable, Expr.Convert(varVar, typeof(object))),
                        Visit(statement.Body),
                        Expr.AddAssign(varVar, stepVar)),
                    scope.BreakLabel());
            return loopExpr;
        }

        BinaryExpression ForLoopBreakCondition(ParameterExpression limitVar, ParameterExpression stepVar, ParameterExpression varVar)
        {
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
            return breakConditionExpr;
        }
    }
}
