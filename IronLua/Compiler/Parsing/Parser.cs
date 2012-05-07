using System;
using System.Collections.Generic;
using System.Diagnostics;
using IronLua.Compiler.Ast;
using IronLua.Library;
using Microsoft.Scripting;
using Microsoft.Scripting.Utils;

namespace IronLua.Compiler.Parsing
{
    class Parser
    {
        const int UNARY_OP_PRIORITY = 8;

        static readonly Dictionary<Symbol, UnaryOp> unaryOps =
            new Dictionary<Symbol, UnaryOp>
                {
                    {Symbol.Minus, UnaryOp.Negate},
                    {Symbol.Not,   UnaryOp.Not},
                    {Symbol.Hash,  UnaryOp.Length}
                };

        static readonly Dictionary<Symbol, BinaryOp> binaryOps =
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

        static readonly Dictionary<BinaryOp, Tuple<int, int>> binaryOpPriorities =
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
        private LuaCompilerOptions _options;

        public Parser(ILexer lexer, ErrorSink errorSink = null, LuaCompilerOptions options = null)
        {
            ContractUtils.RequiresNotNull(lexer, "lexer");

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

        void Consume()
        {
            Current = Next;
            Next = GetNextToken();
        }

        string ConsumeLexeme()
        {
            var lexeme = Current.Lexeme;
            Consume();
            return lexeme;
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

        void Expect(Symbol symbol)
        {
            if (!TryConsume(symbol))
            {
                throw ReportSyntaxErrorNear("'{0}' expected", 
                    symbol.ToTokenString());
            }
        }

        void ExpectMatch(Symbol right, Symbol left, SourceLocation leftStart)
        {
            if (!TryConsume(right))
            {
                if (Current.Line == leftStart.Line)
                {
                    throw ReportSyntaxErrorNear("'{0}' expected", 
                        right.ToTokenString());
                }
                else
                {
                    throw ReportSyntaxErrorNear("'{0}' expected (to close '{1}' at line {2})",
                        right.ToTokenString(), 
                        left.ToTokenString(), 
                        leftStart.Line);
                }
            }
        }

        string ExpectLexeme(Symbol symbol)
        {
            var lexeme = Current.Lexeme;
            Expect(symbol);
            return lexeme;
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
            s += String.Format(" near '{0}'", t.Lexeme ?? t.Symbol.ToTokenString());
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
                ExpectLexeme(Symbol.Identifier)
            };

            while (TryConsume(Symbol.Comma))
                identifiers.Add(ExpectLexeme(Symbol.Identifier));

            return identifiers;
        }

        /* Parses experssionList
         * expr {',' expr } */
        List<Expression> ExpressionList()
        {
            var expressions = new List<Expression>()
            {
                Expression()
            };

            while (TryConsume(Symbol.Comma))
                expressions.Add(Expression());

            return expressions;
        }

        /* Parses variableList
         * (firstVariable | variable) {',' variable } */
        List<Variable> VariableList(Variable firstVariable = null)
        {
            var variables = new List<Variable>()
            {
                firstVariable ?? Variable()
            };

            while (TryConsume(Symbol.Comma))
                variables.Add(Variable());

            return variables;
        }

        /* Parses table construction
         * '{' [field {sep field} [sep]] '}'
         * sep := ',' | ';' */
        Expression.Table TableConstruction()
        {
            var leftStart = Current.Span.Start;
            Expect(Symbol.LeftBrace);

            var fields = new List<Field>();
            do
            {
                if (Current.Symbol == Symbol.RightBrace)
                    break;

                fields.Add(Field());

            } while (TryConsume(Symbol.Comma, Symbol.SemiColon));

            ExpectMatch(Symbol.RightBrace, Symbol.LeftBrace, leftStart);
            return new Expression.Table(fields);
        }

        /* Parses Number */
        double NumberLiteral()
        {
            var number = ExpectLexeme(Symbol.Number);
            double result;
            bool successful = number.StartsWith("0x") || number.StartsWith("0X") ?
                NumberUtil.TryParseHexNumber(number.Substring(2), true, out result) :
                NumberUtil.TryParseDecimalNumber(number, out result);

            if (successful)
                return result;

            // Check if value is well formed!   Stuff like 10e500 return +INF
            var fields = number.Split('e', 'E');
            if (fields.Length == 2)
            {
                int v1, v2;
                bool b1 = Int32.TryParse(fields[0], out v1);
                bool b2 = Int32.TryParse(fields[1], out v2);

                if (b1 && b2)
                {
                    return Math.Sign(v1) > 0 ? Double.PositiveInfinity : Double.NegativeInfinity;
                }
            }

            throw ReportSyntaxErrorNear("malformed number '{0}'", number);
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
                    var memberId = ConsumeLexeme();
                    Expect(Symbol.Equal);
                    return new Field.MemberId(memberId, Expression());

                default:
                    return new Field.Normal(Expression());
            }
        }

        /* Parses arguments
         * '(' expressionList ')' | table | String */
        Arguments Arguments()
        {
            switch (Current.Symbol)
            {
                case Symbol.LeftParen:
                    var leftStart = Current.Span.Start;
                    Consume();

                    var arguments = new List<Expression>();
                    if (Current.Symbol != Symbol.RightParen)
                        arguments = ExpressionList();

                    ExpectMatch(Symbol.RightParen, Symbol.LeftParen, leftStart);
                    return new Arguments.Normal(arguments);

                case Symbol.LeftBrace:
                    return new Arguments.Table(TableConstruction());

                case Symbol.String:
                    var str = new Expression.String(ExpectLexeme(Symbol.String));
                    return new Arguments.String(str);

                default:
                    throw ReportSyntaxErrorNear("function arguments expected");
            }
        }

        /* Parses functionBody
         * '(' [identifierList [',' '...'] | '...'] ')' block 'end' */
        FunctionBody FunctionBody()
        {
            Expect(Symbol.LeftParen);

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
                    parameters.Add(ExpectLexeme(Symbol.Identifier));

                    while (!varargs && TryConsume(Symbol.Comma))
                    {
                        if (Current.Symbol == Symbol.Identifier)
                            parameters.Add(ConsumeLexeme());
                        else if (TryConsume(Symbol.DotDotDot))
                            varargs = true;
                    }
                }
            }

            Expect(Symbol.RightParen);
            var body = Block();
            Expect(Symbol.End);
            return new FunctionBody(parameters, varargs, body);
        }

        /* Parses functionName
         * Identifier {'.' Identifier} [':' Identifer] */
        FunctionName FunctionName()
        {
            var identifiers = new List<string> {ExpectLexeme(Symbol.Identifier)};

            while (TryConsume(Symbol.Dot))
                identifiers.Add(ExpectLexeme(Symbol.Identifier));

            var table = TryConsume(Symbol.Colon) ? ExpectLexeme(Symbol.Identifier) : null;

            return new FunctionName(identifiers, table);
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
                    var leftStart = Current.Span.Start;
                    Expect(Symbol.LeftParen);
                    left = new PrefixExpression.Expression(Expression());
                    ExpectMatch(Symbol.RightParen, Symbol.LeftParen, leftStart);
                    break;
                case Symbol.Identifier:
                    left = new PrefixExpression.Variable(new Variable.Identifier(ConsumeLexeme()));
                    break;
                default:
                    throw ReportSyntaxErrorNear("unexpected symbol");
            }

            /* primaryexpr -> prefixexpr { `.' NAME | `[' expr `]' | `:' NAME funcargs | funcargs } */

            while (true)
            {
                string identifier;
                switch (Current.Symbol)
                {
                    case Symbol.LeftBrack:
                        var leftStart = Current.Span.Start;
                        Expect(Symbol.LeftBrack);
                        left = new PrefixExpression.Variable(new Variable.MemberExpr(left, Expression()));
                        ExpectMatch(Symbol.RightBrack, Symbol.LeftBrack, leftStart);
                        break;

                    case Symbol.Dot:
                        Consume();
                        identifier = ExpectLexeme(Symbol.Identifier);
                        left = new PrefixExpression.Variable(new Variable.MemberId(left, identifier));
                        break;

                    case Symbol.Colon:
                        Consume();
                        identifier = ExpectLexeme(Symbol.Identifier);
                        var arguments = Arguments();
                        left = new PrefixExpression.FunctionCall(new FunctionCall.Table(left, identifier, arguments));
                        break;

                    case Symbol.LeftParen:
                    case Symbol.LeftBrace:
                    case Symbol.String:
                        left = new PrefixExpression.FunctionCall(new FunctionCall.Normal(left, Arguments()));
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
                if (!binaryOps.ContainsKey(Current.Symbol))
                    break;
            }

            return left;
        }

        /* Helper for parsing expressions */
        Expression SimpleExpression()
        {
            switch (Current.Symbol)
            {
                case Symbol.Nil:
                    Consume();
                    return new Expression.Nil();
                case Symbol.True:
                    Consume();
                    return new Expression.Boolean(true);
                case Symbol.False:
                    Consume();
                    return new Expression.Boolean(false);
                case Symbol.Number:
                    return new Expression.Number(NumberLiteral());
                case Symbol.String:
                    return new Expression.String(ConsumeLexeme());
                case Symbol.DotDotDot:
                    Consume();
                    return new Expression.Varargs();
                case Symbol.Function:
                    Consume();
                    return new Expression.Function(FunctionBody());
                case Symbol.Identifier:
                case Symbol.LeftParen:
                    return new Expression.Prefix(PrefixExpression());
                case Symbol.LeftBrace:
                    return TableConstruction();

                default:
                    UnaryOp unaryOp;
                    if (unaryOps.TryGetValue(Current.Symbol, out unaryOp))
                    {
                        Consume();
                        var expression = BinaryExpression(SimpleExpression(), UNARY_OP_PRIORITY);
                        return new Expression.UnaryOp(unaryOp, expression);
                    }
                    throw ReportSyntaxErrorNear("unexpected symbol");
            }
        }

        /* Helper for parsing expressions */
        Expression BinaryExpression(Expression left, int limit)
        {
            BinaryOp binaryOp;
            if (!binaryOps.TryGetValue(Current.Symbol, out binaryOp))
                return left;

            // Recurse while having higher binding
            var priority = binaryOpPriorities[binaryOp];
            if (priority.Item1 < limit)
                return left;

            Consume();
            var right = BinaryExpression(SimpleExpression(), priority.Item2);

            return new Expression.BinaryOp(binaryOp, left, right);
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
            return new Block(StatementsList(terms));
        }

        /* Parses return
         * stat -> 'return' [exprlist] [';'] */
        LastStatement Return(Symbol[] terms)
        {
            Expect(Symbol.Return);

            // Must handle different termination cases:
            //  A) do return end
            //  B) function f() return end
            //  C) if cond then return elseif return else return end
            //  D) repeat return until cond

            var isTerminal = IsTerminalSymbol(terms)
                          || Current.Symbol == Symbol.SemiColon;

            var exprList = !isTerminal ? ExpressionList() : new List<Expression>();

            if (_options.UseLua52Features)
                TryConsume(Symbol.SemiColon);

            return new LastStatement.Return(exprList);
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

            return new Statement.FunctionCall(functionCall);
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

            return new Statement.Assign(variables, expressions);
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
            var identifiers = IdentifierList();

            var values = TryConsume(Symbol.Equal) ? ExpressionList() : null;

            return new Statement.LocalAssign(identifiers, values);
        }

        /* Parses localFunction
         * 'function' Identifier functionBody */
        Statement LocalFunction()
        {
            Expect(Symbol.Function);
            var name = ExpectLexeme(Symbol.Identifier);
            return new Statement.LocalFunction(name, FunctionBody());
        }

        /* Parses function
         * stat -> 'function' functionName functionBody */
        Statement Function()
        {
            Expect(Symbol.Function);
            return new Statement.Function(FunctionName(), FunctionBody());
        }

        /* Parses for
         * stat -> 'for' (forIn | forNormal) */
        Statement For()
        {
            Expect(Symbol.For);
            if (Next.Symbol == Symbol.Comma || Next.Symbol == Symbol.In)
                return ForIn();
            return ForNormal();
        }

        /* Parses forNormal
         * Identifier '=' expression ',' expression [',' expression] 'do' block 'end' */
        Statement ForNormal()
        {
            var identifier = ExpectLexeme(Symbol.Identifier);
            Expect(Symbol.Equal);
            var var = Expression();
            Expect(Symbol.Comma);
            var limit = Expression();
            var step = TryConsume(Symbol.Comma) ? Expression() : null;

            Expect(Symbol.Do);
            var body = Block();
            Expect(Symbol.End);

            return new Statement.For(identifier, var, limit, step, body);
        }

        /* Parses forIn
         * identifierList 'in' expressionList 'do' block 'end' */
        Statement ForIn()
        {
            var identifiers = IdentifierList();
            Expect(Symbol.In);
            var values = ExpressionList();

            Expect(Symbol.Do);
            var body = Block();
            Expect(Symbol.End);

            return new Statement.ForIn(identifiers, values, body);
        }

        /* Parses if
         * stat -> 'if' testThenBody {'elseif' testThenBody} ['else' block] 'end' */
        Statement If()
        {
            Expect(Symbol.If);

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

            Expect(Symbol.End);
            return new Statement.If(iflist, elseBody);
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
            Expect(Symbol.Repeat);
            var body = Block(Symbol.Until);
            Expect(Symbol.Until);
            var test = Expression();
            return new Statement.Repeat(body, test);
        }

        /* Parses while
         * stat -> 'while' expr 'do' block 'end' */
        Statement While()
        {
            Expect(Symbol.While);
            var test = Expression();
            Expect(Symbol.Do);
            var body = Block();
            Expect(Symbol.End);
            return new Statement.While(test, body);
        }

        /* Parses do
         * stat -> 'do' block 'end' */
        Statement Do()
        {
            Expect(Symbol.Do);
            var body = Block();
            Expect(Symbol.End);
            return new Statement.Do(body);
        }

        /* Parses break
         * stat -> 'break' [';'] */
        Statement Break()
        {
            Expect(Symbol.Break);
            if (_options.UseLua52Features)
            {
                TryConsume(Symbol.SemiColon);
                return new Statement.Goto("@break");
            }
            return new LastStatement.Break();
        }

        /* Parses goto
         * stat -> 'goto' identifier */
        Statement Goto()
        {
            Expect(Symbol.Goto);
            var label = ExpectLexeme(Symbol.Identifier);
            return new Statement.Goto(label);
        }

        /* Parses label declaration
         * stat -> '::' identifier '::' */
        Statement Label()
        {
            Expect(Symbol.ColonColon);
            var label = ExpectLexeme(Symbol.Identifier);
            Expect(Symbol.ColonColon);
            return new Statement.LabelDecl(label);
        }
    }
}
