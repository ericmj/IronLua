using System;
using System.IO;
using System.Text;
using Microsoft.Scripting;
using Microsoft.Scripting.Runtime;
using Microsoft.Scripting.Utils;

namespace IronLua.Compiler.Parser
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
        readonly StringBuilder buffer;

        readonly TextReader reader;

        public Input(TextReader reader, string fileName = null)
        {
            ContractUtils.RequiresNotNull(reader, "reader");
            this.reader = reader;
            File = fileName ?? "<unknown>";
            index = 0;
            buffer = new StringBuilder(1024);

            source = reader.ReadToEnd();
        }

        public Input(string source, string fileName = null)
            : this(new StringReader(source), fileName)
        {
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
                    throw SyntaxException(ExceptionMessage.UNEXPECTED_EOF, e);
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
                    throw SyntaxException(ExceptionMessage.UNEXPECTED_EOF, e);
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

        public Token Output(Symbol symbol)
        {
            return new Token(symbol, Line, Column);
        }

        public Token OutputBuffer(Symbol symbol)
        {
            return new Token(symbol, storedLine, storedColumn, buffer.ToString());
        }

        public LuaSyntaxException SyntaxException(string message, Exception inner = null)
        {
            return new LuaSyntaxException(File, Line, Column, message, inner);
        }

        public LuaSyntaxException SyntaxException(string format, params object[] args)
        {
            return SyntaxException(String.Format(format, args));
        }
    }
}
