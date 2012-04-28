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

        private readonly ILexer lexer;
        private ErrorSink _errors;
        private LuaCompilerOptions _options;

        public Parser(ILexer lexer, ErrorSink errorSink, LuaCompilerOptions options = null)
        {
            ContractUtils.RequiresNotNull(lexer, "lexer");
            ContractUtils.RequiresNotNull(errorSink, "errorSink");

            this.lexer = lexer;

            _errors = errorSink;
            _options = options ?? new LuaCompilerOptions();

            // Debug: used to display the sequence of tokens that were read
            //TokenSink = (t, s) => Debug.Print("{0,-12} {1,-10} {2,-10} {3}", t.Symbol, t.Span.Start, t.Span.End, t.Lexeme ?? "");

            // initialise the token management variables
            Last = null;
            Current = GetNextToken();
            Next = GetNextToken();
        }

        #region Token management

        public Action<Token, SourceSpan> TokenSink { get; set; }

        internal Token GetNextToken()
        {
            Token token = lexer.GetNextToken();

            var tokenSink = TokenSink;
            if (tokenSink != null)
                tokenSink(token, SourceSpan.None); // lexer.TokenSpan); //, lexer.TokenLexeme);

            return token;
        }

        Token Last { get; set; }
        Token Current { get; set; }
        Token Next { get; set; }

        Symbol CurrentSymbol { get { return Current.Symbol; } }
        Symbol NextSymbol    { get { return Next.Symbol; } }

        void Consume()
        {
            Last = Current;
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
                throw ReportSyntaxError("'{0}' expected near '{1}'",
                    symbol.ToTokenString(),
                    Current.Lexeme);
            }
        }

        void ExpectMatch(Symbol right, Symbol left, SourceLocation leftStart)
        {
            if (!TryConsume(right))
            {
                if (Current.Line == leftStart.Line)
                {
                    throw ReportSyntaxError("'{0}' expected near '{1}'",
                        right.ToTokenString(),
                        Current.Lexeme);
                }
                else
                {
                    throw ReportSyntaxError("'{0}' expected (to close '{1}' at line {2}) near '{3}'",
                        right.ToTokenString(),
                        left.ToTokenString(),
                        leftStart.Line,
                        Current.Lexeme);
                }
            }
        }

        string ExpectLexeme(Symbol symbol)
        {
            var lexeme = Current.Lexeme;
            Expect(symbol);
            return lexeme;
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

        Exception ReportSyntaxError(string format, params object[] args)
        {
            string msg = String.Format(format, args);
            _errors.Add(lexer.SourceUnit, msg, Current.Span, -1, Severity.Error);
            return lexer.SyntaxException(format, args);
        }

        #endregion

        public Block Parse()
        {
            TryConsume(Symbol.Shebang);
            var block = Block();
            Expect(Symbol.Eof);
            return block;
        }

        /* Parses identifierList
         * identifier {',' identifier} */
        List<string> IdentifierList()
        {
            var identifiers = new List<string> {ExpectLexeme(Symbol.Identifier)};

            while (TryConsume(Symbol.Comma))
                identifiers.Add(ExpectLexeme(Symbol.Identifier));

            return identifiers;
        }

        /* Parses experssionList
         * expression {',' expression } */
        List<Expression> ExpressionList()
        {
            var expressions = new List<Expression> { Expression() };

            while (TryConsume(Symbol.Comma))
                expressions.Add(Expression());

            return expressions;
        }

        /* Parses variableList
         * (oldVariable | variable) {',' variable } */
        List<Variable> VariableList(Variable oldVariable = null)
        {
            var variables = new List<Variable> {oldVariable ?? Variable()};

            while (TryConsume(Symbol.Comma))
                variables.Add(Variable());

            return variables;
        }

        /* Parses table
         * '{' [field {sep field} [sep]] '}'
         * sep := ',' | ';' */
        Expression.Table Table()
        {
            var leftStart = Current.Span.Start;
            Expect(Symbol.LeftBrace);

            var fields = new List<Field>();
            do
            {
                if (CurrentSymbol == Symbol.RightBrace)
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

            throw ReportSyntaxError(ExceptionMessage.MALFORMED_NUMBER, number);
        }


        /* Parses variable
         * Identifier |
         * prefixExpression '[' expression ']' |
         * prefixExpression '.' Identifier */
        Variable Variable()
        {
            var variable = PrefixExpression().LiftVariable();
            if (variable == null)
                throw ReportSyntaxError(ExceptionMessage.UNEXPECTED_SYMBOL, CurrentSymbol);
            return variable;
        }

        /* Parses field
         * '[' expression ']' '=' expression |
         * Identifier '=' expression | 
         * expression */
        Field Field()
        {
            switch (CurrentSymbol)
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

        /* Parses elseif
         * 'elseif' expression 'then' block */
        Elseif Elseif()
        {
            Expect(Symbol.Elseif);
            var test = Expression();
            Expect(Symbol.Then);
            var body = Block(Symbol.Else, Symbol.Elseif);
            return new Elseif(test, body);
        }

        /* Parses arguments
         * '(' expressionList ')' | table | String */
        Arguments Arguments()
        {
            switch (CurrentSymbol)
            {
                case Symbol.LeftParen:
                    var leftStart = Current.Span.Start;
                    Consume();

                    var arguments = new List<Expression>();
                    if (CurrentSymbol != Symbol.RightParen)
                        arguments = ExpressionList();

                    ExpectMatch(Symbol.RightParen, Symbol.LeftParen, leftStart);
                    return new Arguments.Normal(arguments);

                case Symbol.LeftBrace:
                    return new Arguments.Table(Table());

                case Symbol.String:
                    var str = new Expression.String(ExpectLexeme(Symbol.String));
                    return new Arguments.String(str);

                default:
                    throw ReportSyntaxError("function arguments expected near '{0}'", Current.Lexeme);
            }
        }

        /* Parses functionBody
         * '(' [identifierList [',' '...'] | '...'] ')' block 'end' */
        FunctionBody FunctionBody()
        {
            Expect(Symbol.LeftParen);

            var parameters = new List<string>();
            var varargs = false;
            
            if (CurrentSymbol != Symbol.RightParen)
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
                        if (CurrentSymbol == Symbol.Identifier)
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
            // Parse the terminal/first symbol of the prefixExpression
            PrefixExpression left;
            switch (CurrentSymbol)
            {
                case Symbol.Identifier:
                    left = new PrefixExpression.Variable(new Variable.Identifier(ConsumeLexeme()));
                    break;
                case Symbol.LeftParen:
                    Consume();
                    left = new PrefixExpression.Expression(Expression());
                    ExpectLexeme(Symbol.RightParen);
                    break;
                default:
                    throw ReportSyntaxError(ExceptionMessage.UNEXPECTED_SYMBOL, CurrentSymbol);
            }

            while (true)
            {
                string identifier;
                switch (CurrentSymbol)
                {
                    case Symbol.LeftBrack:
                        Consume();
                        left = new PrefixExpression.Variable(new Variable.MemberExpr(left, Expression()));
                        Expect(Symbol.RightBrack);
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
                if (!binaryOps.ContainsKey(CurrentSymbol))
                    break;
            }

            return left;
        }

        /* Helper for parsing expressions */
        Expression SimpleExpression()
        {
            switch (CurrentSymbol)
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
                    return Table();
                
                default:
                    UnaryOp unaryOp;
                    if (unaryOps.TryGetValue(CurrentSymbol, out unaryOp))
                    {
                        Consume();
                        var expression = BinaryExpression(SimpleExpression(), UNARY_OP_PRIORITY);
                        return new Expression.UnaryOp(unaryOp, expression);
                    }
                    throw ReportSyntaxError("unexpected symbol near '{0}'", Current.Lexeme);
            }
        }

        /* Helper for parsing expressions */
        Expression BinaryExpression(Expression left, int limit)
        {
            BinaryOp binaryOp;
            if (!binaryOps.TryGetValue(CurrentSymbol, out binaryOp))
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
         * {do | while | repeat | if | for | function | local | assignOrFunctionCall}
         * [return | break | goto | labelDecl] */
        Block Block(params Symbol[] termSymbols)
        {
            var statements = new List<Statement>();
            LastStatement lastStatement = null;
            bool continueBlock = true;

            while (continueBlock)
            {
                switch (CurrentSymbol)
                {
                    case Symbol.Do:
                        statements.Add(Do());
                        break;
                    case Symbol.While:
                        statements.Add(While());
                        break;
                    case Symbol.Repeat:
                        statements.Add(Repeat());
                        break;
                    case Symbol.If:
                        statements.Add(If());
                        break;
                    case Symbol.For:
                        statements.Add(For());
                        break;
                    case Symbol.Function:
                        statements.Add(Function());
                        break;
                    case Symbol.Local:
                        statements.Add(Local());
                        break;
                    case Symbol.Identifier:
                    case Symbol.LeftParen:
                        statements.Add(AssignOrFunctionCall());
                        break;
                    case Symbol.Goto: // Lua 5.2 feature
                        statements.Add(Goto());
                        break;
                    case Symbol.ColonColon: // Lua 5.2 feature 
                        statements.Add(LabelDecl());
                        break;

                    case Symbol.SemiColon: // Lua 5.2 feature - empty statements
                        if (!_options.UseLua52Features) 
                            goto default;
                        Expect(Symbol.SemiColon);
                        // adds nothing to statements list                        
                        break;

                    case Symbol.Return:
                        lastStatement = Return(termSymbols);
                        continueBlock = false;
                        break;
                    case Symbol.Break:
                        Expect(Symbol.Break);
                        lastStatement = new LastStatement.Break();
                        continueBlock = false;
                        break;
                    default:
                        continueBlock = false;
                        break;
                }

                TryConsume(Symbol.SemiColon);
            }

            return new Block(statements, lastStatement);
        }

        /* Parses return
         * 'return' [expressionList] */
        LastStatement Return(params Symbol[] termSymbol)
        {
            Expect(Symbol.Return);

            if (termSymbol == null || termSymbol.Length <= 0)
                termSymbol = new[] { Symbol.End };

            // Must handle different termination cases:
            //  A) do return end
            //  B) function f() return end
            //  C) if cond then return elseif return else return end
            //  D) repeat return until cond
            // Note: Semicolon symbol is always used as a terminal.

            bool isTermnal = CurrentSymbol == Symbol.SemiColon;

            for (int i = 0; !isTermnal && i < termSymbol.Length; ++i)
            {
                isTermnal = CurrentSymbol == termSymbol[i];
            }

            return new LastStatement.Return(isTermnal
                ? new List<Expression>()
                : ExpressionList());
        }

        /* Parses assignOrFunctionCall
         * assign | functionCall */
        Statement AssignOrFunctionCall()
        {
            var prefixExpr = PrefixExpression();

            switch (CurrentSymbol)
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
                throw ReportSyntaxError(ExceptionMessage.UNEXPECTED_SYMBOL, CurrentSymbol);

            return new Statement.FunctionCall(functionCall);
        }

        /* Parses assign
         * variableList '=' expressionList */
        Statement Assign(PrefixExpression prefixExpr)
        {
            var variable = prefixExpr.LiftVariable();
            if (variable == null)
                throw ReportSyntaxError(ExceptionMessage.UNEXPECTED_SYMBOL, CurrentSymbol);

            var variables = CurrentSymbol == Symbol.Comma ? VariableList(variable) : new List<Variable> {variable};
            Expect(Symbol.Equal);
            var expressions = ExpressionList();

            return new Statement.Assign(variables, expressions);
        }

        /* Parses local
         * 'local' (localFunction | localAssign) */
        Statement Local()
        {
            Expect(Symbol.Local);

            if (CurrentSymbol == Symbol.Function)
                return LocalFunction();
            if (CurrentSymbol == Symbol.Identifier)
                return LocalAssign();

            throw ReportSyntaxError(ExceptionMessage.UNEXPECTED_SYMBOL, CurrentSymbol);
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
            var identifier = ExpectLexeme(Symbol.Identifier);
            return new Statement.LocalFunction(identifier, FunctionBody());
        }

        /* Parses function
         * 'function' functionName functionBody */
        Statement Function()
        {
            Expect(Symbol.Function);
            return new Statement.Function(FunctionName(), FunctionBody());
        }

        /* Parses for
         * 'for' (forIn | forNormal) */
        Statement For()
        {
            Expect(Symbol.For);
            if (NextSymbol == Symbol.Comma || NextSymbol == Symbol.In)
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
         * 'if' expression 'then' block {elseif} ['else' block] 'end' */
        Statement If()
        {
            Expect(Symbol.If);
            var test = Expression();
            Expect(Symbol.Then);
            var body = Block(Symbol.Else, Symbol.Elseif);

            var elseifs = new List<Elseif>();
            while (CurrentSymbol == Symbol.Elseif)
                elseifs.Add(Elseif());

            var elseBody = TryConsume(Symbol.Else)
                ? Block(Symbol.Else, Symbol.Elseif)
                : null;

            Expect(Symbol.End);

            return new Statement.If(test, body, elseifs, elseBody);
        }

        /* Parses repeat
         * 'repeat' block 'until' expression */
        Statement Repeat()
        {
            Expect(Symbol.Repeat);
            var body = Block(Symbol.Until);
            Expect(Symbol.Until);
            var test = Expression();
            return new Statement.Repeat(body, test);
        }

        /* Parses while
         * 'while' expression 'do' block 'end' */
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
         * 'do' block 'end' */
        Statement Do()
        {
            Expect(Symbol.Do);
            var body = Block();
            Expect(Symbol.End);
            return new Statement.Do(body);
        }

        /* Parses goto
         * 'goto' identifier */
        Statement Goto()
        {
            Expect(Symbol.Goto);
            var label = ExpectLexeme(Symbol.Identifier);
            return new Statement.Goto(label);
        }

        /* Parses label declaration
         * '::' identifier '::' */
        Statement LabelDecl()
        {
            Expect(Symbol.ColonColon);
            var label = ExpectLexeme(Symbol.Identifier);
            Expect(Symbol.ColonColon);
            //return new Statement.LabelDecl(label);
            throw new NotImplementedException();
        }
    }
}
