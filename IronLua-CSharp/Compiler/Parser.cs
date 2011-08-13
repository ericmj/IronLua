using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using IronLua_CSharp.Compiler.Ast;

namespace IronLua_CSharp.Compiler
{
    class Parser
    {
        Input input;
        Lexer lexer;

        public Parser(Input input)
        {
            this.input = input;
            lexer = new Lexer(input);
        }

        public Block Parse()
        {
            var block = Block();
            lexer.Expect(Symbol.Eof);
            return block;
        }

        Block Block()
        {
            var statements = new List<Statement>();

            while (true)
            {
                switch (lexer.Current.Symbol)
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
                    case Symbol.Return:
                        return new Block(statements.ToArray(), Return());
                    case Symbol.Break:
                        return new Block(statements.ToArray(), new LastStatement.Break());
                    default:
                        throw new CompileException(input, ExceptionMessage.UNEXPECTED_SYMBOL, lexer.Current.Symbol);
                }

                lexer.TryConsume(Symbol.SemiColon);
            }
        }

        string[] IdentifierList()
        {
            throw new NotImplementedException();
        }

        Expression[] ExpressionList()
        {
            throw new NotImplementedException();
        }

        Variable[] VariableList(Variable variable = null)
        {
            throw new NotImplementedException();
        }

        Elseif Elseif()
        {
            lexer.Expect(Symbol.Elseif);
            var test = Expression();
            lexer.Expect(Symbol.Then);
            var body = Block();
            return new Elseif(test, body);
        }

        PrefixExpression PrefixExpression()
        {
            throw new NotImplementedException();
        }

        Expression Expression()
        {
            throw new NotImplementedException();
        }

        FunctionBody FunctionBody()
        {
            lexer.Expect(Symbol.LeftParen);
            if (lexer.TryConsume(Symbol.RightParen))
                return new FunctionBody(new string[] {}, false, Block());

            var parameters = IdentifierList();
            var varargs = lexer.TryConsume(Symbol.Comma);
            if (varargs) lexer.Expect(Symbol.DotDotDot);
            lexer.Expect(Symbol.RightParen);
            return new FunctionBody(parameters, varargs, Block());
        }

        FunctionName FunctionName()
        {
            var identifiers = new List<string> {lexer.ExpectLexeme(Symbol.Identifier)};

            while (lexer.TryConsume(Symbol.Comma))
                identifiers.Add(lexer.ExpectLexeme(Symbol.Identifier));

            var table = lexer.TryConsume(Symbol.Colon) ? lexer.ExpectLexeme(Symbol.Identifier) : null;

            return new FunctionName(identifiers.ToArray(), table);
        }

        LastStatement Return()
        {
            lexer.Expect(Symbol.Return);
            return new LastStatement.Return(ExpressionList());
        }

        Statement AssignOrFunctionCall()
        {
            var prefixExpr = PrefixExpression();

            switch (lexer.Current.Symbol)
            {
                case Symbol.Comma:
                case Symbol.Equal:
                    return Assign(prefixExpr);
                case Symbol.Colon:
                case Symbol.LeftParen:
                case Symbol.LeftBrace:
                case Symbol.String:
                    return FunctionCall(prefixExpr);
                default:
                    throw new CompileException(input, ExceptionMessage.UNEXPECTED_SYMBOL, lexer.Current.Symbol);
            }
        }

        Statement FunctionCall(PrefixExpression prefixExpr)
        {
            var functionCall = prefixExpr.LiftFunctionCall();
            if (functionCall == null)
                throw new CompileException(input, ExceptionMessage.UNEXPECTED_SYMBOL, lexer.Current.Symbol);

            return new Statement.FunctionCall(functionCall);
        }

        Statement Assign(PrefixExpression prefixExpr)
        {
            var variable = prefixExpr.LiftVariable();
            if (variable == null)
                throw new CompileException(input, ExceptionMessage.UNEXPECTED_SYMBOL, lexer.Current.Symbol);

            var variables = lexer.TryConsume(Symbol.Comma) ? VariableList(variable) : new[] {variable};
            lexer.Expect(Symbol.Equal);
            var expressions = ExpressionList();

            return new Statement.Assign(variables, expressions);
        }

        Statement Local()
        {
            lexer.Expect(Symbol.Local);

            if (lexer.Current.Symbol == Symbol.Function)
                return LocalFunction();
            if (lexer.Current.Symbol == Symbol.Identifier)
                return LocalAssign();

            throw new CompileException(input, ExceptionMessage.UNEXPECTED_SYMBOL, lexer.Current.Symbol);
        }

        Statement LocalAssign()
        {
            var identifiers = IdentifierList();
            var values = lexer.TryConsume(Symbol.Equal) ? ExpressionList() : null;
            return new Statement.LocalAssign(identifiers, values);
        }

        Statement LocalFunction()
        {
            lexer.Expect(Symbol.Function);
            var identifier = lexer.ExpectLexeme(Symbol.Identifier);
            return new Statement.LocalFunction(identifier, FunctionBody());
        }

        Statement Function()
        {
            lexer.Expect(Symbol.Function);
            return new Statement.Function(FunctionName(), FunctionBody());
        }

        Statement For()
        {
            lexer.Expect(Symbol.For);
            if (lexer.Next.Symbol == Symbol.Comma || lexer.Next.Symbol == Symbol.In)
                return ForIn();
            return ForNormal();
        }

        Statement ForNormal()
        {
            var identifier = lexer.ExpectLexeme(Symbol.Identifier);
            lexer.Expect(Symbol.Equal);
            var var = Expression();
            lexer.Expect(Symbol.Comma);
            var limit = Expression();
            var step = lexer.TryConsume(Symbol.Comma) ? Expression() : null;

            lexer.Expect(Symbol.Do);
            var body = Block();
            lexer.Expect(Symbol.End);

            return new Statement.For(identifier, var, limit, step, body);
        }

        Statement ForIn()
        {
            var identifiers = IdentifierList();
            lexer.Expect(Symbol.In);
            var values = ExpressionList();

            lexer.Expect(Symbol.Do);
            var body = Block();
            lexer.Expect(Symbol.End);

            return new Statement.ForIn(identifiers, values, body);
        }

        Statement If()
        {
            lexer.Expect(Symbol.If);
            var test = Expression();
            lexer.Expect(Symbol.Then);
            var body = Block();

            var elseifs = new List<Elseif>();
            while (lexer.Current.Symbol == Symbol.Elseif)
                elseifs.Add(Elseif());

            var elseBody = lexer.TryConsume(Symbol.Else) ? Block() : null;

            return new Statement.If(test, body, elseifs.ToArray(), elseBody);
        }

        Statement Repeat()
        {
            lexer.Expect(Symbol.Repeat);
            var body = Block();
            lexer.Expect(Symbol.Until);
            var test = Expression();
            return new Statement.Repeat(body, test);
        }

        Statement While()
        {
            lexer.Expect(Symbol.While);
            var test = Expression();
            lexer.Expect(Symbol.Do);
            var body = Block();
            lexer.Expect(Symbol.End);
            return new Statement.While(test, body);
        }

        Statement Do()
        {
            lexer.Expect(Symbol.Do);
            var body = Block();
            lexer.Expect(Symbol.End);
            return new Statement.Do(body);
        }
    }
}
