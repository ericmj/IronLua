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
                        throw new CompileException(input, String.Format(ExceptionMessage.UNEXPECTED_SYMBOL,
                                                                        lexer.Current.Symbol));
                }

                lexer.TryConsume(Symbol.SemiColon);
            }
        }

        LastStatement Return()
        {
            throw new NotImplementedException();
        }

        Statement AssignOrFunctionCall()
        {
            throw new NotImplementedException();
        }

        Statement Local()
        {
            throw new NotImplementedException();
        }

        Statement Function()
        {
            throw new NotImplementedException();
        }

        Statement For()
        {
            throw new NotImplementedException();
        }

        Statement If()
        {
            throw new NotImplementedException();
        }

        Statement Repeat()
        {
            throw new NotImplementedException();
        }

        Statement While()
        {
            throw new NotImplementedException();
        }

        Statement Do()
        {
            throw new NotImplementedException();
        }
    }
}
