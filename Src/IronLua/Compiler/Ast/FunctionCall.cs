using Microsoft.Scripting;

namespace IronLua.Compiler.Ast
{
    abstract class FunctionCall : Node
    {
        public SourceSpan Span;

        public abstract T Visit<T>(IFunctionCallVisitor<T> visitor);

        public class Normal : FunctionCall
        {
            public PrefixExpression Prefix { get; private set; }
            public Arguments Arguments { get; private set; }

            public Normal(PrefixExpression prefix, Arguments arguments)
            {
                Prefix = prefix;
                Arguments = arguments;
            }

            public override T Visit<T>(IFunctionCallVisitor<T> visitor)
            {
                return visitor.Visit(this);
            }
        }

        public class Table : FunctionCall.Normal
        {
            public string MethodName { get; private set; }

            public Table(PrefixExpression prefix, string name, Arguments arguments)
                : base(prefix, arguments)
            {
                MethodName = name;
            }

            public override T Visit<T>(IFunctionCallVisitor<T> visitor)
            {
                return visitor.Visit(this);
            }
        }
    }
}