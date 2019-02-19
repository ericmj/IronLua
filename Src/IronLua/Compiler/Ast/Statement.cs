using System.Collections.Generic;
using System.Diagnostics.Contracts;
using Microsoft.Scripting;
using Microsoft.Scripting.Utils;

namespace IronLua.Compiler.Ast
{
    abstract class Statement : Node
    {
        public SourceSpan Span;

        public abstract T Visit<T>(IStatementVisitor<T> visitor);

        public class Assign : Statement
        {
            public List<Variable> Variables { get; private set; }
            public List<Expression> Values { get; private set; }

            public Assign(List<Variable> variables, List<Expression> values)
            {
                Variables = variables;
                Values = values;
            }

            public Assign(Variable variable, Expression value)
                : this(new List<Variable>{ variable }, new List<Expression>{ value })
            {
            }

            public override T Visit<T>(IStatementVisitor<T> visitor)
            {
                return visitor.Visit(this);
            }
        }

        public class LocalAssign : Statement
        {
            public List<string> Identifiers { get; private set; }
            public List<Expression> Values { get; private set; }

            public LocalAssign(List<string> identifiers, List<Expression> values)
            {
                Identifiers = identifiers;
                Values = values;
            }

            public LocalAssign(string identifier, Expression value)
                : this(new List<string> { identifier }, new List<Expression> { value })
            {
            }

            public LocalAssign(string identifier)
                : this(new List<string> { identifier }, null)
            {
            }

            public override T Visit<T>(IStatementVisitor<T> visitor)
            {
                return visitor.Visit(this);
            }
        }

        public class FunctionCall : Statement
        {
            public Ast.FunctionCall Call { get; set; }

            public FunctionCall(Ast.FunctionCall call)
            {
                Call = call;
            }

            public override T Visit<T>(IStatementVisitor<T> visitor)
            {
                return visitor.Visit(this);
            }
        }

        public class Do : Statement
        {
            public Block Body { get; set; }

            public Do(Block body)
            {
                Body = body;
            }

            public override T Visit<T>(IStatementVisitor<T> visitor)
            {
                return visitor.Visit(this);
            }
        }

        public class While : Statement
        {
            public Expression Test { get; set; }
            public Block Body { get; set; }

            public While(Expression test, Block body)
            {
                Test = test;
                Body = body;
            }

            public override T Visit<T>(IStatementVisitor<T> visitor)
            {
                return visitor.Visit(this);
            }
        }

        public class Repeat : Statement
        {
            public Block Body { get; set; }
            public Expression Test { get; set; }

            public Repeat(Block body, Expression test)
            {
                Body = body;
                Test = test;
            }

            public override T Visit<T>(IStatementVisitor<T> visitor)
            {
                return visitor.Visit(this);
            }
        }

        public class If : Statement
        {
            public List<TestThenBody> IfList { get; private set; }
            public Block ElseBody { get; private set; }

            public If(List<TestThenBody> ifList, Block elseBody)
            {
                ContractUtils.RequiresNotEmpty(ifList, "ifList");
                IfList = ifList;
                ElseBody = elseBody;
            }

            public If(Expression test, Block body)
            {
                IfList = new List<TestThenBody>()
                {
                    new TestThenBody(test, body)
                };
                ElseBody = null;
            }

            public class TestThenBody
            {
                public Expression Test { get; private set; }
                public Block Body { get; private set; }

                public TestThenBody(Expression test, Block body)
                {
                    Test = test;
                    Body = body;
                }
            }

            public override T Visit<T>(IStatementVisitor<T> visitor)
            {
                return visitor.Visit(this);
            }
        }

        public class For : Statement
        {
            public string Identifier { get; set; }
            public Expression Var { get; set; }
            public Expression Limit { get; set; }
            public Expression Step { get; set; }
            public Block Body { get; set; }

            public For(string indentifier, Expression var, Expression limit, Expression step, Block body)
            {
                Identifier = indentifier;
                Var = var;
                Limit = limit;
                Step = step;
                Body = body;
            }

            public override T Visit<T>(IStatementVisitor<T> visitor)
            {
                return visitor.Visit(this);
            }
        }

        public class ForIn : Statement
        {
            public List<string> Identifiers { get; set; }
            public List<Expression> Values { get; set; }
            public Block Body { get; set; }

            public ForIn(List<string> identifiers, List<Expression> values, Block body)
            {
                Identifiers = identifiers;
                Values = values;
                Body = body;
            }

            public override T Visit<T>(IStatementVisitor<T> visitor)
            {
                return visitor.Visit(this);
            }
        }

        public class Function : Statement
        {
            public FunctionName Name { get; private set; }
            public FunctionBody Body { get; private set; }

            public Function(FunctionName name, FunctionBody body)
            {
                Contract.Requires(name != null);
                Contract.Requires(body != null);
                Name = name;
                Body = body;
            }

            public virtual bool IsLocal
            {
                get { return false; }
            }

            public override T Visit<T>(IStatementVisitor<T> visitor)
            {
                return visitor.Visit(this);
            }
        }

        public class LocalFunction : Statement.Function
        {
            public LocalFunction(string identifier, FunctionBody body)
                : base(new FunctionName(identifier), body)
            {
            }

            public override bool IsLocal
            {
                get { return true; }
            }

            public override T Visit<T>(IStatementVisitor<T> visitor)
            {
                return visitor.Visit(this);
            }
        }

        public class LabelDecl : Statement
        {
            public string LabelName { get; private set; }

            public LabelDecl(string name)
            {
                LabelName = name;
            }

            public override T Visit<T>(IStatementVisitor<T> visitor)
            {
                return visitor.Visit(this);
            }
        }

        public class Goto : Statement
        {
            public string LabelName { get; private set; }

            public Goto(string label)
            {
                LabelName = label;
            }

            public override T Visit<T>(IStatementVisitor<T> visitor)
            {
                return visitor.Visit(this);
            }
        }
    }
}