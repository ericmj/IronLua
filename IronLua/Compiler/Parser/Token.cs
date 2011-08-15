namespace IronLua.Compiler.Parser
{
    class Token
    {
        public Symbol Symbol { get; private set; }
        public int Line { get; private set; }
        public int Column { get; private set; }
        public string Lexeme { get; private set; }

        public Token(Symbol symbol, int line, int column, string lexeme = null)
        {
            Symbol = symbol;
            Lexeme = lexeme;
            Line = line;
            Column = column;
        }
    }
}