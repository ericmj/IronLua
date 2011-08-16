using System;
using System.Collections.Generic;
using Expr = System.Linq.Expressions.Expression;

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

        public override Expr Compile(Scope scope)
        {
            throw new NotImplementedException();
        }
    }
}