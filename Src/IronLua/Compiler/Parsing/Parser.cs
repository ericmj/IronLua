using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Linq;
using IronLua.Compiler.Ast;
using IronLua.Library;
using Microsoft.Scripting;
using Microsoft.Scripting.Utils;

namespace IronLua.Compiler.Parsing
{
    class Parser
    {
        const int UnaryOpPriority = 8;

        static readonly Dictionary<Symbol, UnaryOp> UnaryOps =
            new Dictionary<Symbol, UnaryOp>
                {
                    {Symbol.Minus, UnaryOp.Negate},
                    {Symbol.Not,   UnaryOp.Not},
                    {Symbol.Hash,  UnaryOp.Length}
                };

        static readonly Dictionary<Symbol, BinaryOp> BinaryOps =
            new Dictionary<Symbol, BinaryOp>
                {
                    {Symbol.Or,           BinaryOp.Or},
                    {Symbol.And,          BinaryOp.And},
                    {Symbol.EqualEqual,   BinaryOp.Equal},
                    {Symbol.TildeEqual,   BinaryOp.NotEqual},
                    {Symbol.Less,         BinaryOp.Less},
                    {Symbol.Greater,      BinaryOp.Greater},
                    {Symbol.LessEqual,    BinaryOp.LessEqual},
                    {Symbol.GreaterEqual, BinaryOp.GreaterEqual},
                    {Symbol.DotDot,       BinaryOp.Concat},
                    {Symbol.Plus,         BinaryOp.Add},
                    {Symbol.Minus,        BinaryOp.Subtract},
                    {Symbol.Star,         BinaryOp.Multiply},
                    {Symbol.Slash,        BinaryOp.Divide},
                    {Symbol.Percent,      BinaryOp.Mod},
                    {Symbol.Caret,        BinaryOp.Power}
                };

        static readonly Dictionary<BinaryOp, Tuple<int, int>> BinaryOpPriorities =
            new Dictionary<BinaryOp, Tuple<int, int>>
                {
                    {BinaryOp.Or,           new Tuple<int, int>(1, 1)},
                    {BinaryOp.And,          new Tuple<int, int>(2, 2)},
                    {BinaryOp.Less,         new Tuple<int, int>(3, 3)},
                    {BinaryOp.Greater,      new Tuple<int, int>(3, 3)},
                    {BinaryOp.LessEqual,    new Tuple<int, int>(3, 3)},
                    {BinaryOp.GreaterEqual, new Tuple<int, int>(3, 3)},
                    {BinaryOp.NotEqual,     new Tuple<int, int>(3, 3)},
                    {BinaryOp.Equal,        new Tuple<int, int>(3, 3)},
                    {BinaryOp.Concat,       new Tuple<int, int>(5, 4)}, // Left associative
                    {BinaryOp.Add,          new Tuple<int, int>(6, 6)},
                    {BinaryOp.Subtract,     new Tuple<int, int>(6, 6)},
                    {BinaryOp.Multiply,     new Tuple<int, int>(7, 7)},
                    {BinaryOp.Divide,       new Tuple<int, int>(7, 7)},
                    {BinaryOp.Mod,          new Tuple<int, int>(7, 7)},
                    {BinaryOp.Power,        new Tuple<int, int>(9, 8)}  // Left associative
                };

        private readonly ILexer _lexer;
        private ErrorSink _errors;
        private readonly LuaCompilerOptions _options;

        public Parser(ILexer lexer, ErrorSink errorSink = null, LuaCompilerOptions options = null)
        {
            Contract.Requires(lexer != null);

            _lexer = lexer;
            _errors = errorSink ?? ErrorSink.Default;
            _options = options ?? LuaCompilerOptions.Default;

            // Debug: used to display the sequence of tokens that were read
            //TokenSink = (t, s) => Debug.Print("{0,-12} {1,-10} {2,-10} {3}", t.Symbol, s.Start, s.End, t.Lexeme);

            // initialise the token management variables
            Current = GetNextToken();
            Next = GetNextToken();
        }

        #region Token management

        public Action<Token, SourceSpan> TokenSink { get; set; }

        internal Token GetNextToken()
        {
            Token token;
            do
            {
                token = _lexer.GetNextToken();
                // ignore the following tokens
            } while (token.Symbol == Symbol.Whitespace ||
                     token.Symbol == Symbol.Comment ||
                     token.Symbol == Symbol.Eol);

            var tokenSink = TokenSink;
            if (tokenSink != null)
                tokenSink(token, token.Span);

            return token;
        }

        Token Current { get; set; }
        Token Next { get; set; }

        Token Consume()
        {
            var token = Current;
            Current = Next;
            Next = GetNextToken();
            return token;
        }

        bool TryConsume(Symbol symbol, out Token token)
        {
            if (Current.Symbol == symbol)
            {
                token = Consume();
                return true;
            }
            token = null;
            return false;
        }

        bool TryConsume(Symbol symbol)
        {
            if (Current.Symbol == symbol)
            {
                Consume();
                return true;
            }
            return false;
        }

        bool TryConsume(params Symbol[] symbols)
        {
            for (int i = 0; i < symbols.Length; ++i)
            {
                if (Current.Symbol == symbols[i])
                {
                    Consume();
                    return true;
                }
            }
            return false;
        }

        Token Expect(Symbol symbol)
        {
            if (Current.Symbol != symbol)
            {
                throw ReportSyntaxErrorNear("'{0}' expected",
                    symbol.ToTokenString());
            }
            return Consume();
        }

        Token ExpectMatch(Symbol right, Symbol left, SourceLocation leftStart)
        {
            if (Current.Symbol != right)
            {
                if (Current.Line == leftStart.Line)
                {
                    throw ReportSyntaxErrorNear("'{0}' expected",
                        right.ToTokenString());
                }

                throw ReportSyntaxErrorNear("'{0}' expected (to close '{1}' at line {2})",
                    right.ToTokenString(),
                    left.ToTokenString(),
                    leftStart.Line);
            }
            return Consume();
        }

        public bool IsTerminalSymbol(Symbol[] terms)
        {
            var symbol = Current.Symbol;

            if (terms == null || terms.Length <= 0)
                terms = new[] { Symbol.End };

            for (int i = 0; i < terms.Length; ++i)
            {
                if (symbol == terms[i])
                    return true;
            }

            return false;
        }

        #endregion

        #region Error reporting

        public ErrorSink ErrorSink
        {
            get { return _errors; }
            set
            {
                ContractUtils.RequiresNotNull(value, "value");
                _errors = value;
            }
        }

        Exception ReportError(int errorCode, string message)
        {
            _errors.Add(_lexer.SourceUnit, message, Current.Span, errorCode, Severity.Error);
            return _lexer.SyntaxException(message);
        }

        Exception ReportSyntaxError(string format, params object[] args)
        {
            return ReportError(1, String.Format(format, args));
        }

        Exception ReportSyntaxErrorNear(string format, params object[] args)
        {
            var t = Current;
            var s = String.Format(format, args);
            s += String.Format(" near '{0}' (line {1}, column {2})", t.Lexeme ?? t.Symbol.ToTokenString(), t.Line, t.Column);
            return ReportError(2, s);
        }

        #endregion

        public Block Parse()
        {
            var block = Block(Symbol.Eof);
            Expect(Symbol.Eof);
            return block;
        }

        /* Parses identifierList
         * identifier {',' identifier} */
        List<string> IdentifierList()
        {
            var identifiers = new List<string>()
            {
                Expect(Symbol.Identifier).Lexeme
            };

            while (TryConsume(Symbol.Comma))
                identifiers.Add(Expect(Symbol.Identifier).Lexeme);

            return identifiers;
        }

        /* Parses experssionList
         * expr {',' expr } */
        List<Expression> ExpressionList(List<Expression> expressions = null)
        {
            if (expressions == null)
                expressions = new List<Expression>();

            if (expressions.Count < 1)
                expressions.Add(Expression());

            while (TryConsume(Symbol.Comma))
                expressions.Add(Expression());

            return expressions;
        }

        /* Parses variableList
         * variable {',' variable } */
        List<Variable> VariableList(List<Variable> variables = null)
        {
            if (variables == null)
                variables = new List<Variable>();

            if (variables.Count < 1)
                variables.Add(Variable());

            while (TryConsume(Symbol.Comma))
                variables.Add(Variable());

            return variables;
        }

        List<Variable> VariableList(Variable firstVariable)
        {
            return VariableList(new List<Variable> { firstVariable });
        }

        /* Parses table construction
         * '{' [field {sep field} [sep]] '}'
         * sep := ',' | ';' */
        Expression.Table TableConstruction()
        {
            var leftSpan = Expect(Symbol.LeftBrace).Span;

            var fields = new List<Field>();
            do
            {
                if (Current.Symbol == Symbol.RightBrace)
                    break;

                fields.Add(Field());

            } while (TryConsume(Symbol.Comma, Symbol.SemiColon));

            var rightSpan = ExpectMatch(Symbol.RightBrace, Symbol.LeftBrace, leftSpan.Start).Span;
            return new Expression.Table(fields)
            {
                Span = new SourceSpan(leftSpan.Start, rightSpan.End)
            };
        }

        /* Parses a number literal
         * number */
        Expression.Number NumberLiteral()
        {
            var token = Expect(Symbol.Number);
            var number = token.Lexeme;

            double result;
            bool successful = number.StartsWith("0x") || number.StartsWith("0X") ?
                NumberUtil.TryParseHexNumber(number.Substring(2), true, out result) :
                NumberUtil.TryParseDecimalNumber(number, out result);

            if (successful)
                return new Expression.Number(result) { Span = token.Span };

            // Check if value is well formed!   Stuff like 10e500 return +INF
            var fields = number.Split('e', 'E');
            if (fields.Length == 2)
            {
                int v1, v2;
                bool b1 = Int32.TryParse(fields[0], out v1);
                bool b2 = Int32.TryParse(fields[1], out v2);

                if (b1 && b2)
                {
                    result = Math.Sign(v1) > 0 ? Double.PositiveInfinity : Double.NegativeInfinity;
                    return new Expression.Number(result) { Span = token.Span };
                }
            }

            throw ReportSyntaxErrorNear("malformed number '{0}'", number);
        }

        /* Parses a string literal
         * string */
        Expression.String StringLiteral()
        {
            var token = Expect(Symbol.String);
            return new Expression.String(token.Lexeme)
            {
                Span = token.Span
            };
        }

        /* Parses variable
         * Identifier |
         * prefixExpression '[' expression ']' |
         * prefixExpression '.' Identifier */
        Variable Variable()
        {
            var variable = PrefixExpression().LiftVariable();
            if (variable == null)
                throw ReportSyntaxError(ExceptionMessage.UNEXPECTED_SYMBOL, Current.Symbol);
            return variable;
        }

        /* Parses field
         * '[' expression ']' '=' expression |
         * Identifier '=' expression |
         * expression */
        Field Field()
        {
            switch (Current.Symbol)
            {
                case Symbol.LeftBrack:
                    Consume();
                    var memberExpr = Expression();
                    Expect(Symbol.RightBrack);
                    Expect(Symbol.Equal);
                    var value = Expression();
                    return new Field.MemberExpr(memberExpr, value);

                case Symbol.Identifier:
                    if (Next.Symbol != Symbol.Equal)
                        goto default;
                    var memberId = Consume().Lexeme;
                    Expect(Symbol.Equal);
                    return new Field.MemberId(memberId, Expression());

                default:
                    return new Field.Normal(Expression());
            }
        }

        /* Parses arguments
         * '(' expressionList ')' | table | string */
        Arguments Arguments()
        {
            switch (Current.Symbol)
            {
                case Symbol.LeftParen:
                    var leftSpan = Consume().Span;

                    var arguments = new List<Expression>();
                    if (Current.Symbol != Symbol.RightParen)
                        ExpressionList(arguments);

                    var rightSpan = ExpectMatch(Symbol.RightParen, Symbol.LeftParen, leftSpan.Start).Span;
                    return new Arguments.Normal(arguments, new SourceSpan(leftSpan.Start, rightSpan.End));

                case Symbol.LeftBrace:
                    return new Arguments.Table(TableConstruction());

                case Symbol.String:
                    return new Arguments.String(StringLiteral());

                default:
                    throw ReportSyntaxErrorNear("function arguments expected");
            }
        }

        /* Parses functionBody
         * '(' [identifierList [',' '...'] | '...'] ')' block 'end' */
        FunctionBody FunctionBody()
        {
            var leftSpan = Expect(Symbol.LeftParen).Span;

            var parameters = new List<string>();
            var varargs = false;

            if (Current.Symbol != Symbol.RightParen)
            {
                if (TryConsume(Symbol.DotDotDot))
                {
                    varargs = true;
                }
                else
                {
                    parameters.Add(Expect(Symbol.Identifier).Lexeme);

                    while (!varargs && TryConsume(Symbol.Comma))
                    {
                        Token name;
                        if (TryConsume(Symbol.Identifier, out name))
                            parameters.Add(name.Lexeme);
                        else if (TryConsume(Symbol.DotDotDot))
                            varargs = true;
                    }
                }
            }

            Expect(Symbol.RightParen);
            var body = Block();

            var rightSpan = Expect(Symbol.End).Span;
            return new FunctionBody(parameters, varargs, body)
            {
                Span = new SourceSpan(leftSpan.Start, rightSpan.End)
            };
        }

        /* Parses functionName
         * Identifier {'.' Identifier} [':' Identifer] */
        FunctionName FunctionName()
        {
            var identifiers = new List<string> { Expect(Symbol.Identifier).Lexeme };

            while (TryConsume(Symbol.Dot))
                identifiers.Add(Expect(Symbol.Identifier).Lexeme);

            var lastIsTableMethod = TryConsume(Symbol.Colon);
            if (lastIsTableMethod)
                identifiers.Add(Expect(Symbol.Identifier).Lexeme);

            return new FunctionName(identifiers, lastIsTableMethod);
        }

        /* Parses prefixExpression
         * variable | functionCall | '(' expression ')' */
        PrefixExpression PrefixExpression()
        {
            /* prefixexpr -> NAME | '(' expr ')' */

            // Parse the terminal/first symbol of the prefixExpression
            PrefixExpression left;
            switch (Current.Symbol)
            {
                case Symbol.LeftParen:
                    var leftSpan = Expect(Symbol.LeftParen).Span;
                    var expr = Expression();
                    var rightSpan = ExpectMatch(Symbol.RightParen, Symbol.LeftParen, leftSpan.Start).Span;
                    left = new PrefixExpression.Expression(expr, new SourceSpan(leftSpan.Start, rightSpan.End));
                    break;
                case Symbol.Identifier:
                    var identifier = Consume();
                    var idVar = new Variable.Identifier(identifier.Lexeme) { Span = identifier.Span };
                    left = new PrefixExpression.Variable(idVar);
                    break;
                default:
                    throw ReportSyntaxErrorNear("unexpected symbol");
            }

            /* primaryexpr -> prefixexpr { `.' NAME | `[' expr `]' | `:' NAME funcargs | funcargs } */

            while (true)
            {
                Token identifier;
                switch (Current.Symbol)
                {
                    case Symbol.LeftBrack:
                        var leftSpan = Expect(Symbol.LeftBrack).Span;
                        var expr = Expression();
                        var rightSpan = ExpectMatch(Symbol.RightBrack, Symbol.LeftBrack, leftSpan.Start).Span;
                        var memberExpr = new Variable.MemberExpr(left, expr)
                        {
                            Span = new SourceSpan(left.Span.Start, rightSpan.End)
                        };
                        left = new PrefixExpression.Variable(memberExpr);
                        break;

                    case Symbol.Dot:
                        Consume();
                        identifier = Expect(Symbol.Identifier);
                        var memberId = new Variable.MemberId(left, identifier.Lexeme)
                        {
                            Span = new SourceSpan(left.Span.Start, identifier.Span.End)
                        };
                        left = new PrefixExpression.Variable(memberId);
                        break;

                    case Symbol.Colon:
                        Consume();
                        identifier = Expect(Symbol.Identifier);
                        var arguments = Arguments();
                        left = new PrefixExpression.FunctionCall(new FunctionCall.Table(left, identifier.Lexeme, arguments)
                        {
                            Span = new SourceSpan(left.Span.Start, arguments.Span.End)
                        });
                        break;

                    case Symbol.LeftParen:
                    case Symbol.LeftBrace:
                    case Symbol.String:
                        var args = Arguments();
                        left = new PrefixExpression.FunctionCall(new FunctionCall.Normal(left, args)
                        {
                            Span = new SourceSpan(left.Span.Start, args.Span.End)
                        });
                        break;

                    // Unrecognized symbol, return what we have so far
                    default:
                        return left;
                }
            }
        }

        /* Parses expression
         * 'nil' | 'true' | 'false' | Number | String | '...' | function | prefixExpression |
         * table | expression BinaryOp expression | UnaryOp expression */
        Expression Expression()
        {
            var left = SimpleExpression();

            while (true)
            {
                left = BinaryExpression(left, 0);
                if (!BinaryOps.ContainsKey(Current.Symbol))
                    break;
            }

            return left;
        }

        /* Helper for parsing expressions */
        Expression SimpleExpression()
        {
            SourceSpan span = Current.Span;

            switch (Current.Symbol)
            {
                case Symbol.Nil:
                    Consume();
                    return new Expression.Nil() { Span = span };
                case Symbol.True:
                    Consume();
                    return new Expression.Boolean(true) { Span = span };
                case Symbol.False:
                    Consume();
                    return new Expression.Boolean(false) { Span = span };
                case Symbol.DotDotDot:
                    Consume();
                    return new Expression.Varargs() { Span = span };
                case Symbol.Number:
                    return NumberLiteral();
                case Symbol.String:
                    return StringLiteral();
                case Symbol.LeftBrace:
                    return TableConstruction();
                case Symbol.Function:
                    Consume();
                    var funcBody = FunctionBody();
                    return new Expression.Function(funcBody)
                    {
                        Span = new SourceSpan(span.Start, funcBody.Span.End)
                    };
                case Symbol.Identifier:
                case Symbol.LeftParen:
                    var prefixExpr = PrefixExpression();
                    return new Expression.Prefix(prefixExpr)
                    {
                        Span = prefixExpr.Span
                    };
                default:
                    UnaryOp unaryOp;
                    if (UnaryOps.TryGetValue(Current.Symbol, out unaryOp))
                    {
                        Consume();
                        var expression = BinaryExpression(SimpleExpression(), UnaryOpPriority);
                        return new Expression.UnaryOp(unaryOp, expression)
                        {
                            Span = new SourceSpan(span.Start, expression.Span.End)
                        };
                    }
                    throw ReportSyntaxErrorNear("unexpected symbol");
            }
        }

        /* Helper for parsing expressions */
        Expression BinaryExpression(Expression left, int limit)
        {
            BinaryOp binaryOp;
            if (!BinaryOps.TryGetValue(Current.Symbol, out binaryOp))
                return left;

            // Recurse while having higher binding
            var priority = BinaryOpPriorities[binaryOp];
            if (priority.Item1 < limit)
                return left;

            Consume();
            var right = BinaryExpression(SimpleExpression(), priority.Item2);

            return new Expression.BinaryOp(binaryOp, left, right)
            {
                Span = new SourceSpan(left.Span.Start, right.Span.End)
            };
        }

        /* Parses block
         * {if | while | do | for | repeat | function | local | labelDecl |
         *  goto | assignOrFunctionCall} [return | break] */
        Statement Statement(Symbol[] terms)
        {
            switch (Current.Symbol)
            {
                case Symbol.If:         return If();
                case Symbol.While:      return While();
                case Symbol.Do:         return Do();
                case Symbol.For:        return For();
                case Symbol.Repeat:     return Repeat();
                case Symbol.Function:   return Function();
                case Symbol.Local:      return Local();
                case Symbol.Return:     return Return(terms);
                case Symbol.Break:      return Break();
                case Symbol.ColonColon: return Label();
                case Symbol.Goto:       return Goto();
                case Symbol.SemiColon:  // Lua 5.2 feature
                    if (!_options.UseLua52Features)
                        goto default;
                    Expect(Symbol.SemiColon);
                    return null;
                default:
                    return AssignOrFunctionCall();
            }
        }

        List<Statement> StatementsList(Symbol[] terms)
        {
            var statList = new List<Statement>();

            while (!IsTerminalSymbol(terms))
            {
                var stat = Statement(terms);
                if (stat == null)
                    continue;

                statList.Add(stat);

                if (!_options.UseLua52Features)
                    TryConsume(Symbol.SemiColon);

                if (stat is LastStatement)
                    break;
            }

            return statList;
        }

        Block Block(params Symbol[] terms)
        {
            var leftSpan = Current.Span;
            var statements = StatementsList(terms);
            return new Block(statements)
            {
                Span = (statements.Count > 0)
                     ? new SourceSpan(statements.First().Span.Start,
                                      statements.Last().Span.End)
                     : leftSpan
            };
        }

        /* Parses return
         * stat -> 'return' [exprlist] [';'] */
        LastStatement Return(Symbol[] terms)
        {
            var leftSpan = Expect(Symbol.Return).Span;

            // Must handle different termination cases:
            //  A) do return end
            //  B) function f() return end
            //  C) if cond then return elseif return else return end
            //  D) repeat return until cond

            var exprList = new List<Expression>();
            var isTerminal = IsTerminalSymbol(terms)
                          || Current.Symbol == Symbol.SemiColon;
            if (!isTerminal)
                ExpressionList(exprList);

            var rightSpan = (exprList.Count > 0) ? exprList.Last().Span : leftSpan;

            if (_options.UseLua52Features)
                TryConsume(Symbol.SemiColon);

            return new LastStatement.Return(exprList)
            {
                Span = new SourceSpan(leftSpan.Start, rightSpan.End)
            };
        }

        /* Parses assignOrFunctionCall (aka. ExprStat)
         * stat -> assignment | functionCall */
        Statement AssignOrFunctionCall()
        {
            /* stat -> func | assignment */
            var prefixExpr = PrefixExpression();

            switch (Current.Symbol)
            {
                case Symbol.Comma:
                case Symbol.Equal:
                    return Assign(prefixExpr);
                default:
                    return FunctionCall(prefixExpr);
            }
        }

        /* Parses functionCall
         * prefixExpression arguments |
         * prefixExpression ':' Identifier arguments */
        Statement FunctionCall(PrefixExpression prefixExpr)
        {
            var functionCall = prefixExpr.LiftFunctionCall();
            if (functionCall == null)
                throw ReportSyntaxError(ExceptionMessage.UNEXPECTED_SYMBOL, Current.Symbol);

            return new Statement.FunctionCall(functionCall)
            {
                Span = functionCall.Span
            };
        }

        /* Parses assign
         * variableList '=' expressionList */
        Statement Assign(PrefixExpression prefixExpr)
        {
            var variable = prefixExpr.LiftVariable();
            if (variable == null)
                throw ReportSyntaxError(ExceptionMessage.UNEXPECTED_SYMBOL, Current.Symbol);

            var variables = VariableList(variable);
            Expect(Symbol.Equal);
            var expressions = ExpressionList();

            Debug.Assert(variables.Count > 0);
            Debug.Assert(expressions.Count > 0);

            return new Statement.Assign(variables, expressions)
            {
                Span = new SourceSpan(variables.First().Span.Start,
                                      expressions.Last().Span.End)
            };
        }

        /* Parses local
         * stat -> 'local' (localFunction | localAssign) */
        Statement Local()
        {
            Expect(Symbol.Local);

            switch (Current.Symbol)
            {
                case Symbol.Function:
                    return LocalFunction();
                case Symbol.Identifier:
                    return LocalAssign();
                default:
                    throw ReportSyntaxErrorNear("{0} expected",
                        Symbol.Identifier.ToTokenString());
            }
        }

        /* Parses localAssign
         * identifierList ['=' expressionList] */
        Statement LocalAssign()
        {
            var leftSpan = Current.Span;

            var identifiers = IdentifierList();

            List<Expression> values = null;
            if (TryConsume(Symbol.Equal))
                values = ExpressionList();

            var rightSpan = Current.Span;
            /*
            var rightSpan = (values != null && values.Count > 0)
                          ? values.Last().Span
                          //: identifiers.Last().Span; // FIMXE: this is wat we want
                          : Current.Span;
                          // FIXME: Want span of last element in identifiers list
             */
            return new Statement.LocalAssign(identifiers, values)
            {
                Span = new SourceSpan(leftSpan.Start, rightSpan.End)
            };
        }

        /* Parses localFunction
         * 'function' Identifier functionBody */
        Statement LocalFunction()
        {
            var leftSpan = Expect(Symbol.Function).Span;
            var funcName = Expect(Symbol.Identifier).Lexeme;
            var funcBody = FunctionBody();
            return new Statement.LocalFunction(funcName, funcBody)
            {
                Span = new SourceSpan(leftSpan.Start, funcBody.Span.End)
            };
        }

        /* Parses function
         * stat -> 'function' functionName functionBody */
        Statement Function()
        {
            var leftSpan = Expect(Symbol.Function).Span;
            var funcName = FunctionName();
            var funcBody = FunctionBody();
            return new Statement.Function(funcName, funcBody)
            {
                Span = new SourceSpan(leftSpan.Start, funcBody.Span.End)
            };
        }

        /* Parses for
         * stat -> 'for' (forIn | forNormal) */
        Statement For()
        {
            var leftSpan = Expect(Symbol.For).Span;
            if (Next.Symbol == Symbol.Comma ||
                Next.Symbol == Symbol.In)
                return ForIn(leftSpan);

            return ForNormal(leftSpan);
        }

        /* Parses forNormal
         * Identifier '=' expression ',' expression [',' expression] 'do' block 'end' */
        Statement ForNormal(SourceSpan leftSpan)
        {
            var identifier = Expect(Symbol.Identifier).Lexeme;
            Expect(Symbol.Equal);
            var var = Expression();
            Expect(Symbol.Comma);
            var limit = Expression();
            var step = TryConsume(Symbol.Comma) ? Expression() : null;
            var body = DoBlockEnd();
            return new Statement.For(identifier, var, limit, step, body)
            {
                Span = new SourceSpan(leftSpan.Start, body.Span.End)
            };
        }

        /* Parses forIn
         * identifierList 'in' expressionList 'do' block 'end' */
        Statement ForIn(SourceSpan leftSpan)
        {
            var identifiers = IdentifierList();
            Expect(Symbol.In);
            var values = ExpressionList();
            var body = DoBlockEnd();
            return new Statement.ForIn(identifiers, values, body)
            {
                Span = new SourceSpan(leftSpan.Start, body.Span.End)
            };
        }

        /* Parses a block
         * 'do' block 'end'
         */
        Block DoBlockEnd()
        {
            var leftStart = Expect(Symbol.Do).Span.Start;
            var block = Block();
            var rightEnd = Expect(Symbol.End).Span.End;
            block.Span = new SourceSpan(leftStart, rightEnd);
            return block;
        }

        /* Parses if
         * stat -> 'if' testThenBody {'elseif' testThenBody} ['else' block] 'end' */
        Statement If()
        {
            var start = Expect(Symbol.If).Span.Start;

            var iflist = new List<Statement.If.TestThenBody>()
            {
                TestThenBody()
            };

            while (TryConsume(Symbol.Elseif))
            {
                iflist.Add(TestThenBody());
            }

            var elseBody = TryConsume(Symbol.Else)
                ? Block(Symbol.End)
                : null;

            var end = Expect(Symbol.End).Span.End;
            return new Statement.If(iflist, elseBody)
            {
                Span = new SourceSpan(start, end)
            };
        }

        /* Parses then
         * test 'then' block */
        Statement.If.TestThenBody TestThenBody()
        {
            var test = Expression();
            Expect(Symbol.Then);
            var body = Block(Symbol.End, Symbol.Else, Symbol.Elseif);
            return new Statement.If.TestThenBody(test, body);
        }

        /* Parses repeat
         * stat -> 'repeat' block 'until' expr */
        Statement Repeat()
        {
            var leftSpan = Expect(Symbol.Repeat).Span;
            var body = Block(Symbol.Until);
            Expect(Symbol.Until);
            var test = Expression();
            return new Statement.Repeat(body, test)
            {
                Span = new SourceSpan(leftSpan.Start, test.Span.End)
            };
        }

        /* Parses while
         * stat -> 'while' expr 'do' block 'end' */
        Statement While()
        {
            var leftSpan = Expect(Symbol.While).Span;
            var test = Expression();
            var body = DoBlockEnd();
            return new Statement.While(test, body)
            {
                Span = new SourceSpan(leftSpan.Start, body.Span.End)
            };
        }

        /* Parses do
         * stat -> 'do' block 'end' */
        Statement Do()
        {
            var body = DoBlockEnd();
            return new Statement.Do(body)
            {
                Span = body.Span
            };
        }

        /* Parses break
         * stat -> 'break' [';'] */
        Statement Break()
        {
            var token = Expect(Symbol.Break);
            if (_options.UseLua52Features)
            {
                TryConsume(Symbol.SemiColon);
                return new Statement.Goto("@break")
                {
                    Span = token.Span
                };
            }
            return new LastStatement.Break()
            {
                Span = token.Span
            };
        }

        /* Parses goto
         * stat -> 'goto' identifier */
        Statement Goto()
        {
            var leftSpan = Expect(Symbol.Goto).Span;
            var label = Expect(Symbol.Identifier);
            return new Statement.Goto(label.Lexeme)
            {
                Span = new SourceSpan(leftSpan.Start, label.Span.End)
            };
        }

        /* Parses label declaration
         * stat -> '::' identifier '::' */
        Statement Label()
        {
            var leftSpan = Expect(Symbol.ColonColon).Span;
            var label = Expect(Symbol.Identifier).Lexeme;
            var rightSpan = Expect(Symbol.ColonColon).Span;
            return new Statement.LabelDecl(label)
            {
                Span = new SourceSpan(leftSpan.Start, rightSpan.End)
            };
        }
    }
}
