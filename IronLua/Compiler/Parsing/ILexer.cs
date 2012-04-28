using Microsoft.Scripting;

namespace IronLua.Compiler.Parsing
{
    internal interface ILexer
    {
        Token GetNextToken();

        LuaSyntaxException SyntaxException(string format, params object[] args);

        SourceUnit SourceUnit { get; }
    }
}
