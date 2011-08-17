using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using IronLua.Util;
using Expr = System.Linq.Expressions.Expression;

namespace IronLua.Compiler.Ast
{
    abstract class Statement : Node
    {
        public class Assign : Statement
        {
            public List<Variable> Variables { get; set; }
            public List<Expression> Values { get; set; }

            public Assign(List<Variable> variables, List<Expression> values)
            {
                Variables = variables;
                Values = values;
            }

            public override Expr Compile(Scope scope)
            {
                throw new NotImplementedException();
            }
        }

        public class FunctionCall : Statement
        {
            public Ast.FunctionCall Call { get; set; }

            public FunctionCall(Ast.FunctionCall call)
            {
                Call = call;
            }

            public override Expr Compile(Scope scope)
            {
                throw new NotImplementedException();
            }
        }

        public class Do : Statement
        {
            public Block Body { get; set; }

            public Do(Block body)
            {
                Body = body;
            }

            public override Expr Compile(Scope scope)
            {
                throw new NotImplementedException();
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

            public override Expr Compile(Scope scope)
            {
                throw new NotImplementedException();
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

            public override Expr Compile(Scope scope)
            {
                throw new NotImplementedException();
            }
        }

        public class If : Statement
        {
            public Expression Test { get; set; }
            public Block Body { get; set; }
            public List<Elseif> Elseifs { get; set; }
            public Block ElseBody { get; set; }

            public If(Expression test, Block body, List<Elseif> elseifs, Block elseBody)
            {
                Test = test;
                Body = body;
                Elseifs = elseifs;
                ElseBody = elseBody;
            }

            public override Expr Compile(Scope scope)
            {
                throw new NotImplementedException();
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

            public override Expr Compile(Scope scope)
            {
                throw new NotImplementedException();
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

            public override Expr Compile(Scope scope)
            {
                throw new NotImplementedException();
            }
        }

        public class Function : Statement
        {
            public FunctionName Name { get; set; }
            public FunctionBody Body { get; set; }

            public Function(FunctionName name, FunctionBody body)
            {
                Name = name;
                Body = body;
            }

            public override Expr Compile(Scope scope)
            {
                throw new NotImplementedException();
            }
        }

        public class LocalFunction : Statement
        {
            public string Identifier { get; set; }
            public FunctionBody Body { get; set; }

            public LocalFunction(string identifier, FunctionBody body)
            {
                Identifier = identifier;
                Body = body;
            }

            public override Expr Compile(Scope scope)
            {
                throw new NotImplementedException();
            }
        }

        public class LocalAssign : Statement
        {
            public List<string> Identifiers { get; set; }
            public List<Expression> Values { get; set; }

            public LocalAssign(List<string> identifiers, List<Expression> values)
            {
                Identifiers = identifiers;
                Values = values;
            }

            public override Expr Compile(Scope scope)
            {
                // Assign values to temporaries
                var valuesCompiled = Values.Select(val => val.Compile(scope)).ToList();
                var tempVariables = valuesCompiled.Select(expr => Expr.Variable(expr.Type)).ToList();
                var tempAssigns = tempVariables.Zip(valuesCompiled, Expr.Assign);

                // Shrink or pad temporary's list with nil to match local's list length
                // and cast temporaries to locals type
                var locals = Identifiers.Select(scope.AddLocal).ToList();
                var tempVariablesResized = tempVariables
                    .Resize(Identifiers.Count, Expression.Nil.Constant.Compile(null))
                    .Zip(locals, (tempVar, local) => Expr.Convert(tempVar, local.Type));

                // Assign temporaries to locals
                var realAssigns = locals.Zip(tempVariablesResized, Expr.Assign);
                return Expr.Block(tempVariables.Concat(locals), tempAssigns.Concat(realAssigns));
            }
        }
    }
}