using System;
using Microsoft.Scripting;

namespace IronLua.Compiler.Parsing
{
    public class Token
    {
        public Symbol Symbol { get; private set; }
        public SourceSpan Span { get; private set; }
        public string Lexeme { get; private set; }
        
        public int Line { get { return Span.Start.Line; } }
        public int Column { get { return Span.Start.Column; } }

        public Token(Symbol symbol, SourceSpan span, string lexeme = null)
        {
            Symbol = symbol;
            Span = span;
            Lexeme = lexeme;
        }
    }
}