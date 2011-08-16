namespace IronLua.Compiler.Ast
{
    abstract class Statement : Node
    {
        public class Assign : Statement
        {
            public Variable[] Variables { get; private set; }
            public Expression[] Values { get; private set; }

            public Assign(Variable[] variables, Expression[] values)
            {
                Variables = variables;
                Values = values;
            }
        }

        public class FunctionCall : Statement
        {
            public Ast.FunctionCall Call { get; private set; }

            public FunctionCall(Ast.FunctionCall call)
            {
                Call = call;
            }
        }

        public class Do : Statement
        {
            public Block Body { get; private set; }

            public Do(Block body)
            {
                Body = body;
            }
        }

        public class While : Statement
        {
            public Expression Test { get; private set; }
            public Block Body { get; private set; }

            public While(Expression test, Block body)
            {
                Test = test;
                Body = body;
            }
        }

        public class Repeat : Statement
        {
            public Block Body { get; private set; }
            public Expression Test { get; private set; }

            public Repeat(Block body, Expression test)
            {
                Body = body;
                Test = test;
            }
        }

        public class If : Statement
        {
            public Expression Test { get; private set; }
            public Block Body { get; private set; }
            public Elseif[] Elseifs { get; private set; }
            public Block ElseBody { get; private set; }

            public If(Expression test, Block body, Elseif[] elseifs, Block elseBody)
            {
                Test = test;
                Body = body;
                Elseifs = elseifs;
                ElseBody = elseBody;
            }
        }

        public class For : Statement
        {
            public string Identifier { get; private set; }
            public Expression Var { get; private set; }
            public Expression Limit { get; private set; }
            public Expression Step { get; private set; }
            public Block Body { get; private set; }

            public For(string indentifier, Expression var, Expression limit, Expression step, Block body)
            {
                Identifier = indentifier;
                Var = var;
                Limit = limit;
                Step = step;
                Body = body;
            }
        }

        public class ForIn : Statement
        {
            public string[] Identifiers { get; private set; }
            public Expression[] Values { get; private set; }
            public Block Body { get; private set; }

            public ForIn(string[] identifiers, Expression[] values, Block body)
            {
                Identifiers = identifiers;
                Values = values;
                Body = body;
            }
        }

        public class Function : Statement
        {
            public FunctionName Name { get; private set; }
            public FunctionBody Body { get; private set; }

            public Function(FunctionName name, FunctionBody body)
            {
                Name = name;
                Body = body;
            }
        }

        public class LocalFunction : Statement
        {
            public string Identifier { get; private set; }
            public FunctionBody Body { get; private set; }

            public LocalFunction(string identifier, FunctionBody body)
            {
                Identifier = identifier;
                Body = body;
            }
        }

        public class LocalAssign : Statement
        {
            public string[] Identifiers { get; private set; }
            public Expression[] Values { get; private set; }

            public LocalAssign(string[] identifiers, Expression[] values)
            {
                Identifiers = identifiers;
                Values = values;
            }
        }
    }
}