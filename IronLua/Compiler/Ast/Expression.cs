using System;
using System.Collections.Generic;
using Expr = System.Linq.Expressions.Expression;

namespace IronLua.Compiler.Ast
{
    abstract class Expression : Node
    {
        public class Nil : Expression
        {
            public override Expr Compile(Scope scope)
            {
                throw new NotImplementedException();
            }
        }
        
        public class Boolean : Expression
        {
            public bool Literal { get; set; }
            
            public Boolean(bool literal)
            {
                Literal = literal;
            }

            public override Expr Compile(Scope scope)
            {
                throw new NotImplementedException();
            }
        }
        
        public class Number : Expression
        {
            public double Literal { get; set; }
            
            public Number(double literal)
            {
                Literal = literal;
            }

            public override Expr Compile(Scope scope)
            {
                throw new NotImplementedException();
            }
        }
        
        public class String : Expression
        {
            public string Literal { get; set; }
            
            public String(string literal)
            {
                Literal = literal;
            }

            public override Expr Compile(Scope scope)
            {
                throw new NotImplementedException();
            }
        }
        
        public class Varargs : Expression
        {
            public override Expr Compile(Scope scope)
            {
                throw new NotImplementedException();
            }
        }
        
        public class Function : Expression
        {
            public FunctionBody Body { get; set; }
            
            public Function(FunctionBody body)
            {
                Body = body;
            }

            public override Expr Compile(Scope scope)
            {
                throw new NotImplementedException();
            }
        }
        
        public class Prefix : Expression
        {
            public PrefixExpression Expression { get; set; }
            
            public Prefix(PrefixExpression expression)
            {
                Expression = expression;
            }

            public override Expr Compile(Scope scope)
            {
                throw new NotImplementedException();
            }
        }
        
        public class Table : Expression
        {
            public List<Field> Fields { get; set; }
            
            public Table(List<Field> fields)
            {
                Fields = fields;
            }

            public override Expr Compile(Scope scope)
            {
                throw new NotImplementedException();
            }
        }
        
        public class BinaryOp : Expression
        {
            public Ast.BinaryOp Operation { get; set; }
            public Expression Left { get; set; }
            public Expression Right { get; set; }
            
            public BinaryOp(Ast.BinaryOp operation, Expression left, Expression right)
            {
                Operation = operation;
                Left = left;
                Right = right;
            }

            public override Expr Compile(Scope scope)
            {
                throw new NotImplementedException();
            }
        }
        
        public class UnaryOp : Expression
        {
            public Ast.UnaryOp Operation { get; set; }
            public Expression Operand { get; set; }
            
            public UnaryOp(Ast.UnaryOp operation, Expression operand)
            {
                Operation = operation;
                Operand = operand;
            }

            public override Expr Compile(Scope scope)
            {
                throw new NotImplementedException();
            }
        }
    }
}