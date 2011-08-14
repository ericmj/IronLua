using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IronLua.Compiler
{
    class Input
    {
        readonly string source;
        int index;

        public string File { get; private set; }
        public int Line { get; private set; }
        public int Column { get; private set; }

        int storedLine;
        int storedColumn;
        StringBuilder buffer;

        public Input(string source)
        {
            this.source = source;
            File = "<unknown>";
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
                    throw new CompileException(this, ExceptionMessage.UNEXPECTED_EOF, e);
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
                    throw new CompileException(this, ExceptionMessage.UNEXPECTED_EOF, e);
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
            Column = Column + 1;
        }

        public void Skip(int n)
        {
            index += n;
            Column = Column + n;
        }

        public void Back()
        {
            index -= 1;
            Column = Column - 1;
        }

        public void StorePosition()
        {
            storedLine = Line;
            storedColumn = Column;
        }

        public void Newline()
        {
            Line += 1;
            Column = 1;
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
            return new Lexer.Token(symbol, Line, Column);
        }

        public Lexer.Token OutputBuffer(Symbol symbol)
        {
            return new Lexer.Token(symbol, storedLine, storedColumn, buffer.ToString());
        }
    }
}
