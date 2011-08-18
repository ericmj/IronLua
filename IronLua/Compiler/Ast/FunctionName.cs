using System.Collections.Generic;

namespace IronLua.Compiler.Ast
{
    class FunctionName : Node
    {
        public List<string> Identifiers { get; set; }
        public string Table { get; set; }

        public FunctionName(List<string> identifiers, string table)
        {
            Identifiers = identifiers;
            Table = table;
        }
    }
}