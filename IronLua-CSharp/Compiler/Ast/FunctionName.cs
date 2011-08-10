namespace IronLua_CSharp.Compiler.Ast
{
    class FunctionName : Node
    {
        public string[] Names { get; private set; }
        public string Table { get; private set; }

        public FunctionName(string[] names, string table)
        {
            Names = names;
            Table = table;
        }
    }
}