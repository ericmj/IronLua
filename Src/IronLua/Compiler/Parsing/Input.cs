using System;
using System.IO;
using System.Text;
using Microsoft.Scripting;
using Microsoft.Scripting.Runtime;
using Microsoft.Scripting.Utils;

namespace IronLua.Compiler.Parsing
{
    class Input
    {
        readonly string source;
        int index;

        public string File { get; private set; }
        public int Line { get; private set; }
        public int Column { get; private set; }

        int storedIndex;
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
                    throw new LuaSyntaxException(File, Line, Column, ExceptionMessage.UNEXPECTED_EOF, e);
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
                    throw new LuaSyntaxException(File, Line, Column, ExceptionMessage.UNEXPECTED_EOF, e);
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
            storedIndex = index;
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
            var loc = new SourceLocation(index, Line, Column);            
            return new Token(symbol, new SourceSpan(loc, loc));
        }

        public Token OutputBuffer(Symbol symbol)
        {
            var loc1 = new SourceLocation(storedIndex, storedLine, storedColumn);
            var loc2 = new SourceLocation(index, Line, Column);
            return new Token(symbol, new SourceSpan(loc1, loc2), buffer.ToString());
        }

        public LuaSyntaxException SyntaxException(string message, Exception inner = null)
        {
            return new LuaSyntaxException(File, Line, Column, message, inner);
        }
    }
}
