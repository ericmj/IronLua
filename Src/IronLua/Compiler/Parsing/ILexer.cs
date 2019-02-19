using Microsoft.Scripting;

namespace IronLua.Compiler.Parsing
{
    internal interface ILexer
    {
        Token GetNextToken();

        LuaSyntaxException SyntaxException(string message);

        SourceUnit SourceUnit { get; }
    }
}
