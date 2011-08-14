namespace IronLua.Compiler.Ast
{
    class Expression : Node
    {
        public class Nil : Expression
        {
        }
        
        public class Boolean : Expression
        {
            public bool Literal { get; private set; }
            
            public Boolean(bool literal)
            {
                Literal = literal;
            }
        }
        
        public class Number : Expression
        {
            public double Literal { get; private set; }
            
            public Number(double literal)
            {
                Literal = literal;
            }
        }
        
        public class String : Expression
        {
            public string Literal { get; private set; }
            
            public String(string literal)
            {
                Literal = literal;
            }
        }
        
        public class Varargs : Expression
        {
        }
        
        public class Function : Expression
        {
            public FunctionBody Body { get; private set; }
            
            public Function(FunctionBody body)
            {
                Body = body;
            }
        }
        
        public class Prefix : Expression
        {
            public PrefixExpression Expression { get; private set; }
            
            public Prefix(PrefixExpression expression)
            {
                Expression = expression;
            }
        }
        
        public class Table : Expression
        {
            public Field[] Fields { get; private set; }
            
            public Table(Field[] fields)
            {
                Fields = fields;
            }
        }
        
        public class BinaryOp : Expression
        {
            public Ast.BinaryOp Operation { get; private set; }
            public Expression Left { get; private set; }
            public Expression Right { get; private set; }
            
            public BinaryOp(Ast.BinaryOp operation, Expression left, Expression right)
            {
                Operation = operation;
                Left = left;
                Right = right;
            }
        }
        
        public class UnaryOp : Expression
        {
            public Ast.UnaryOp Operation { get; private set; }
            public Expression Operand { get; private set; }
            
            public UnaryOp(Ast.UnaryOp operation, Expression operand)
            {
                Operation = operation;
                Operand = operand;
            }
        }
    }
}