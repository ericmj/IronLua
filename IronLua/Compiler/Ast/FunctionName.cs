using System;
using System.Collections.Generic;
using LinqExpression = System.Linq.Expressions.Expression;

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

        public override LinqExpression Compile(Scope scope)
        {
            throw new NotImplementedException();
        }
    }
}