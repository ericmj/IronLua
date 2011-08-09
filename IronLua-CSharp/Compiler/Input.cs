using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IronLua_CSharp.Compiler
{
    class Input
    {
        readonly string file;
        readonly string source;
        int index;

        int line;
        int column;

        int storedLine;
        int storedColumn;
        StringBuilder buffer;

        public Input(string source)
        {
            this.source = source;
            file = "<unknown>";
            index = 0;
            buffer = new StringBuilder(1024);
        }

        public char Current
        {
            get
            {
                try
                {
                    return source[index];
                }
                catch (IndexOutOfRangeException e)
                {
                    throw new CompileException(file, line, column, ExceptionMessage.UNEXPECTED_EOF, e);
                }
            }
        }

        public char Peek
        {
            get
            {
                try
                {
                    return source[index + 1];
                }
                catch (IndexOutOfRangeException e)
                {
                    throw new CompileException(file, line, column, ExceptionMessage.UNEXPECTED_EOF, e);
                }
            }
        }

        public bool CanContinue
        {
            get { return index < source.Length; }
        }

        public bool CanPeek
        {
            get { return index + 1 < source.Length; }
        }

        public string Buffer
        {
            get { return buffer.ToString(); }
        }

        public void Advance()
        {
            index += 1;
            column += 1;
        }

        public void Skip(int n)
        {
            index += n;
            column += n;
        }

        public void Back()
        {
            index -= 1;
            column -= 1;
        }

        public void StorePosition()
        {
            storedLine = line;
            storedColumn = column;
        }

        public void Newline()
        {
            line += 1;
            column = 1;
        }

        public void BufferAppend(char c)
        {
            buffer.Append(c);
        }

        public void BufferAppend(string str)
        {
            buffer.Append(str);
        }

        public void BufferRemove(int length)
        {
            buffer.Remove(buffer.Length - length, length);
        }

        public void BufferRemove(int start, int length)
        {
            buffer.Remove(start, length);
        }

        public void BufferClear()
        {
            buffer.Clear();
        }

        public Lexer.Token Output(Symbol symbol)
        {
            return new Lexer.Token(symbol, line, column);
        }

        public Lexer.Token OutputBuffer(Symbol symbol)
        {
            return new Lexer.Token(symbol, line, column, buffer.ToString());
        }
    }
}
