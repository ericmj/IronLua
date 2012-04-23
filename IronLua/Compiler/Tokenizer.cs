using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using IronLua.Compiler.Parser;
using IronLua.Util;
using Microsoft.Scripting;
using Microsoft.Scripting.Runtime;
using Microsoft.Scripting.Utils;

namespace IronLua.Compiler
{
    [Serializable]
    public class LuaCompilerOptions : CompilerOptions
    {
        public bool SkipFirstLine { get; set; }
        public bool MultiEolns { get; set; }
        public bool UseLua52Features { get; set; }
        public int InitialBufferCapacity { get; set; }

        public LuaCompilerOptions()
        {
            SkipFirstLine = true;
            MultiEolns = true;
            UseLua52Features = true;
            InitialBufferCapacity = 1024;
        }        
    }    

    public class Tokenizer : TokenizerService, ILexer
    {
        private static readonly Dictionary<string, Symbol> Keywords =
            new Dictionary<string, Symbol>
            {
                {"and", Symbol.And},
                {"break", Symbol.Break},
                {"do", Symbol.Do},
                {"else", Symbol.Else},
                {"elseif", Symbol.Elseif},
                {"end", Symbol.End},
                {"false", Symbol.False},
                {"for", Symbol.For},
                {"function", Symbol.Function},
                {"goto", Symbol.Goto},
                {"if", Symbol.If},
                {"in", Symbol.In},
                {"local", Symbol.Local},
                {"nil", Symbol.Nil},
                {"not", Symbol.Not},
                {"or", Symbol.Or},
                {"repeat", Symbol.Repeat},
                {"return", Symbol.Return},
                {"then", Symbol.Then},
                {"true", Symbol.True},
                {"until", Symbol.Until},
                {"while", Symbol.While}
            };

        private SourceUnit _sourceUnit;
        private ErrorSink _errors;
        private Tokenizer.State _state;
        private TokenizerBuffer _buffer;

        private SourceSpan _lastTokenSpan;
        private string _lastTokenValue;

        private readonly LuaCompilerOptions _options;
        
        public Tokenizer(ErrorSink errorListner, LuaCompilerOptions options)            
        {
            ContractUtils.RequiresNotNull(errorListner, "errorSink");
            ContractUtils.RequiresNotNull(options, "options");

            _errors = errorListner;
            _options = options;
        }

        public override void Initialize(object state, TextReader sourceReader, SourceUnit sourceUnit, SourceLocation initialLocation)
        {
            ContractUtils.RequiresNotNull(sourceReader, "sourceReader");

            _sourceUnit = sourceUnit;
            _state = new State(state as State);
                        
            _buffer = new TokenizerBuffer(sourceReader, initialLocation, _options.InitialBufferCapacity, _options.MultiEolns);

            if (_options.SkipFirstLine)
            {
                if (_buffer.Peek() == '#' )//&& _buffer.GetChar(+1) == '!')
                    _buffer.ReadLine(); // skip shibang                
            }
        }

        public Token GetNextToken()
        {
            Symbol symbol;
            do
            {
                symbol = GetNextSymbol();

            } while (symbol == Symbol.Whitespace ||
                     symbol == Symbol.Comment ||
                     symbol == Symbol.Eol);

            return new Token(symbol, _lastTokenSpan, _lastTokenValue);
        }

        public SourceSpan GetTokenSpan()
        {
            return _lastTokenSpan;
        }

        public string GetTokenValue()
        {
            return _lastTokenValue;
        }

        Symbol MarkTokenEnd(Symbol symbol, Func<string> getTokenValue = null, bool isMultiLine = false)
        {
            _buffer.MarkTokenEnd(isMultiLine);
            _lastTokenSpan = _buffer.TokenSpan;
            _lastTokenValue = (getTokenValue == null) ? null : getTokenValue();
            return symbol;
        }

        Symbol MarkKeywordTokenEnd(Symbol symbol, Func<string> getTokenValue)
        {
            ContractUtils.RequiresNotNull(getTokenValue, "getTokenValue");

            _buffer.MarkSingleLineTokenEnd();
            _lastTokenSpan = _buffer.TokenSpan;
            _lastTokenValue = getTokenValue();

            // Keyword or identifier?
            Symbol result;
            if (!Keywords.TryGetValue(_lastTokenValue, out result))
                result = symbol;
            else
                _lastTokenValue = null; // Keywords don't have a value

            return result;
        }

        internal Symbol GetNextSymbol()
        {
            _buffer.DiscardToken();

            int current = Read();

            if (current == TokenizerBuffer.EndOfFile)
            {
                return MarkTokenEnd(Symbol.EndOfStream);
            }
            else if (current.IsIdentifierStart())
            {
                ConsumeMany(x => x.IsIdentifier());
                return MarkKeywordTokenEnd(Symbol.Identifier, 
                    () => _buffer.GetTokenString());
            }
            else if (current.IsDecimal())
            {
                return ScanNumericLiteral((char)current);
            }
            else if (current == '\'' || current == '"')
            {
                return ScanStringLiteral((char)current);
            }
            else if (current == '[')
            {
                int next = Peek();
                // [ may start a long literal string ([[) or be a token on its own
                return (next == '[' || next == '=')
                    ? ScanLongStringLiteral((char)current)
                    : MarkTokenEnd(Symbol.LeftBrack);
            }
            else if (current == '-')
            {
                return (Peek() == '-')
                    ? ScanCommentLiteral((char)current)
                    : MarkTokenEnd(Symbol.Minus);
            }
            else if (current.IsPunctuation())
            {
                return ScanPunctuation((char)current);
            }
            else if (current.IsWhitespace())
            {
                ConsumeMany(x => x.IsWhitespace());
                return MarkTokenEnd(Symbol.Whitespace);
            }
            else if (_buffer.ReadEolnOpt(current) > 0)
            {                
                return MarkTokenEnd(Symbol.EndOfLine, isMultiLine: true);
            }
            else
            {
                _buffer.MarkSingleLineTokenEnd();
                ReportError(1, "invalid character '{0}'", (char)current);
                return Symbol.Error;
            }
        }

        #region Punctuation

        Symbol ScanPunctuation(char c)
        {
            switch (c)
            {
                case '+':
                    return MarkTokenEnd(Symbol.Plus);
                case '-':
                    return MarkTokenEnd(Symbol.Minus);
                case '*':
                    return MarkTokenEnd(Symbol.Star);
                case '/':
                    return MarkTokenEnd(Symbol.Slash);
                case '%':
                    return MarkTokenEnd(Symbol.Percent);
                case '^':
                    return MarkTokenEnd(Symbol.Caret);
                case '#':
                    return MarkTokenEnd(Symbol.Hash);
                case '(':
                    return MarkTokenEnd(Symbol.LeftParen);
                case ')':
                    return MarkTokenEnd(Symbol.RightParen);
                case '{':
                    return MarkTokenEnd(Symbol.LeftBrace);
                case '}':
                    return MarkTokenEnd(Symbol.RightBrace);
                case '[':
                    return MarkTokenEnd(Symbol.LeftBrack);
                case ']':
                    return MarkTokenEnd(Symbol.RightBrack);
                case ';':
                    return MarkTokenEnd(Symbol.SemiColon);
                case ',':
                    return MarkTokenEnd(Symbol.Comma);
                case '~':
                case '<':
                case '>':
                case '=':
                case ':':
                case '.':
                    return ScanLongPunctuation(c);
                default:
                    throw Assert.Unreachable;
            }
        }

        Symbol ScanLongPunctuation(char c)
        {
            switch (c)
            {
                case '~':
                    return ConsumeOne('=')
                        ? MarkTokenEnd(Symbol.TildeEqual)
                        : MarkTokenEnd(Symbol.Tilde);
                case '<':
                    return ConsumeOne('=')
                        ? MarkTokenEnd(Symbol.LessEqual)
                        : MarkTokenEnd(Symbol.Less);
                case '>':
                    return ConsumeOne('=')
                        ? MarkTokenEnd(Symbol.GreaterEqual)
                        : MarkTokenEnd(Symbol.Greater);
                case '=':
                    return ConsumeOne('=')
                        ? MarkTokenEnd(Symbol.EqualEqual)
                        : MarkTokenEnd(Symbol.Equal);
                case ':':
                    return _options.UseLua52Features && ConsumeOne(':')
                        ? MarkTokenEnd(Symbol.ColonColon)
                        : MarkTokenEnd(Symbol.Colon);
                case '.':
                    return ConsumeOne('.')
                        ? ConsumeOne('.') 
                            ? MarkTokenEnd(Symbol.DotDotDot)
                            : MarkTokenEnd(Symbol.DotDot)
                        : Peek().IsDecimal()
                            ? ScanNumericLiteral(c) 
                            : MarkTokenEnd(Symbol.Dot); 
                default:
                    throw Assert.Unreachable;
            }
        }

        #endregion

        #region String Literal

        // Scans a simple string literal (i.e. one quoted with ' or ")
        Symbol ScanStringLiteral(char quote)
        {
            Debug.Assert(quote == '\'' || quote == '"');

            var accum = new StringBuilder();
            bool isMultiLine = false;
            bool skipWs = false;
            bool done = false;
            do
            {
                int current = Read();

                switch (current)
                {
                    case TokenizerBuffer.EndOfFile:
                        _buffer.MarkSingleLineTokenEnd();
                        ReportError(3, "Unterminated quoted string meets end of file");                        
                        //return new IncompleteStringToken(accum.ToString(), (quoteChar == '\''));
                        return Symbol.Error;

                    case '\r':
                    case '\n':
                        if (skipWs) continue;
                        _buffer.MarkSingleLineTokenEnd(-1);
                        ReportError(2, "unfinished string at '{0}'", _buffer.GetTokenString());
                        return Symbol.Error;

                    case ' ':
                    case '\t':
                    case '\f':
                    case '\v':
                        if (skipWs) continue;
                        accum.Append((char)current);
                        break;

                    case '\\':
                        skipWs = false;
                        current = Read();
                        // Escape sequence
                        switch (current)
                        {
                            case 'a':  current = '\a'; break;
                            case 'b':  current = '\b'; break;
                            case 'f':  current = '\f'; break;
                            case 'n':  current = '\n'; break;
                            case 'r':  current = '\r'; break;
                            case 't':  current = '\t'; break;
                            case 'v':  current = '\v'; break;
                            
                            case '\"': current = '\"'; break;
                            case '\'': current = '\''; break;
                            case '\\': current = '\\'; break;

                            case '\r':
                            case '\n': 
                                isMultiLine = true;
                                break;

                            case '0':
                            case '1':
                            case '2':
                            case '3':
                            case '4':
                            case '5':
                            case '6':
                            case '7':
                            case '8':
                            case '9':
                                /* a numeric escape sequence, such as \012 or \9 */
                                int v = current - '0';
                                for (int i = 1; i < 3; ++i)
                                {
                                    if (!Peek().IsDecimal())
                                        break;
                                    v = (v * 10) + (Read() - '0');
                                }
                                current = v;
                                break;

                            case 'x': // a hexadecimal escape sequence (Lua 5.2 feature)
                                if (!_options.UseLua52Features) goto default;
                                int offset = _buffer.TokenRelativePosition;
                                int value = 0;
                                for (int i = 0; i < 2; ++i)
                                {
                                    current = Read();
                                    if ('0' <= current && current <= '9')
                                        value = (value << 4) | (current - '0');
                                    else if ('a' <= current && current <= 'f')
                                        value = (value << 4) | (current - 'a' + 10);
                                    else if ('A' <= current && current <= 'F')
                                        value = (value << 4) | (current - 'A' + 10);
                                    else
                                    {
                                        _buffer.MarkSingleLineTokenEnd();
                                        ReportError(4, "hexadecimal digit expected near '{0}'", _buffer.GetTokenSubstring(offset));
                                        return Symbol.Error;
                                    }
                                }
                                current = value;
                                break;

                            case 'z':
                                if (!_options.UseLua52Features) goto default;
                                skipWs = true; // from hear on, skip all whitespace (Lua 5.2 feature)
                                continue;                                

                            default:
                                break;

                            // Lua manual says only the above chars can be 
                            // escaped but Luac allows any char to be escaped.
                        }
                        accum.Append((char)current);
                        break;

                    case '\'':
                    case '"':
                        // Is it the closing quote?
                        done = (current == quote);
                        if (done) break;
                        goto default;
                    default:
                        accum.Append((char)current);
                        skipWs = false;
                        break;
                }           
            } while (!done);
            
            return MarkTokenEnd(Symbol.String, accum.ToString, isMultiLine);
        }

        Symbol ScanLongStringLiteral(char c, bool isComment = false)
        {
            Debug.Assert(c == '[');

            int numEqualsStart = ConsumeMany('=');
            int numEqualsEnd = -1;
            
            if (!ConsumeOne('['))
            {
                if (isComment)
                {
                    _buffer.ReadLine(); // just continue as a normal short comment
                    return MarkTokenEnd(Symbol.Comment, () => _buffer.GetTokenSubstring(2));
                }
                
                _buffer.MarkSingleLineTokenEnd();
                ReportError(5, "invalid long string delimiter at '{0}'", _buffer.GetTokenString());
                return Symbol.Error;
            }

            ConsumeOneEol(); // Skip newline immediately following the start

            var accum = new StringBuilder();
            bool done = false;
            do
            {
                int current = Read();
                
                switch (current)
                {
                    case TokenizerBuffer.EndOfFile:
                        _buffer.MarkMultiLineTokenEnd();
                        ReportError(5, "invalid long {0} delimiter at '{1}'",
                            isComment ? "comment" : "string", _buffer.GetTokenString());                                        
                        return Symbol.Error;

                    case '\r': // need to replace \r\n sequence into \n ones
                        if (Peek() == '\n')
                            continue;
                        break;

                    case '=':
                        if (numEqualsEnd >= 0)
                        {
                            numEqualsEnd++;
                            done = (numEqualsEnd == numEqualsStart) && (Peek() == ']');
                        }
                        break;
                        
                    case ']':
                        numEqualsEnd = 0;
                        done = (numEqualsEnd == numEqualsStart) && (Peek() == ']');
                        break;

                    default:
                        numEqualsEnd = -1;
                        break;
                }
                
                accum.Append((char) current);
            } while (!done);

            if (!ConsumeOne(']'))
            {   
                _buffer.MarkSingleLineTokenEnd();
                ReportError(5, "invalid long string delimiter at '{0}'", _buffer.GetTokenString());
                return Symbol.Error;

                throw Assert.Unreachable; // should really be this!
            }

            return MarkTokenEnd(isComment ? Symbol.Comment : Symbol.String, 
                () => accum.ToString(0, accum.Length - 1 - numEqualsEnd),
                isMultiLine:true);
        }

        #endregion

        #region Numeric Literal

        Symbol ScanNumericLiteral(char c)
        {
            Debug.Assert(c.IsDecimal() || c == '.');

            bool isHex;
            int mantissa;
            int fraction = -1;
            int exponent = -1;
            char expChar;
            int leftovers;
            Func<int, bool> numberCheck;
            Func<int, bool> exponentCheck;
            
            if (c == '0' && ConsumeOne(x => x == 'x' || x == 'X'))
            {
                // Prepare for a hexadecimal number
                numberCheck = x => x.IsHex();
                exponentCheck = x => (x == 'p' || x == 'P');
                isHex = true;
                mantissa = 0;
            }
            else
            {
                // Prepare for a decimal number
                numberCheck = x => x.IsDecimal();
                exponentCheck = x => (x == 'e' || x == 'E');
                isHex = false;
                mantissa = c.IsDecimal() ? 1 : 0;
            }

            // Go ahead and scan the number
            mantissa += ConsumeMany(numberCheck);
            if (c != '.' && ConsumeOne('.'))
            {
                fraction = ConsumeMany(numberCheck);
            }
            expChar = Char.ToLowerInvariant((char)_buffer.Peek());
            if (ConsumeOne(exponentCheck))
            {
                ConsumeOne(x => x == '-' || x == '+');
                exponent = ConsumeMany(x => x.IsDecimal());
            }
            leftovers = ConsumeMany(x => x.IsIdentifier());

            bool bad = false;
            bad |= leftovers > 0; // leftover characters
            bad |= mantissa <= 0 && fraction <= 0; // no mantissa or fraction values
            bad |= exponent == 0; // missing exponent value
            bad |= exponent > 0 && ((isHex && expChar != 'p') || (!isHex && expChar != 'e'));
            if (bad)
            {                
                _buffer.MarkSingleLineTokenEnd();
                ReportError(6, "malformed number near '{0}'", _buffer.GetTokenString());
                return Symbol.Error;
            }

            return MarkTokenEnd(Symbol.Number, () => _buffer.GetTokenString());
        }

        #endregion

        #region Comments

        /// <summary>
        /// Scans a comment, either short form "-- comment" or long form "--[[ comment ]]"
        /// </summary>
        Symbol ScanCommentLiteral(char c)
        {
            char c1 = c;                    // first -
            char c2 = (char)Read(); // second -
            Debug.Assert(c1 == '-' && c2 == '-');

            if (ConsumeOne('['))
            {
                // long comment of the form: [[comment string]] or [=[comment string]=]
                return ScanLongStringLiteral('[', isComment: true);
            }
            else
            {
                // Handle a short comment literal
                _buffer.ReadLine();
                return MarkTokenEnd(Symbol.Comment, () => _buffer.GetTokenSubstring(2));
            }
        }

        #endregion

        #region Peek, Read & Consume functions

        int Peek()
        {
            return _buffer.Peek();
        }

        int Read()
        {
            return _buffer.Read();                        
        }
        
        int ConsumeMany(char c)
        {
            int count = 0;

            while (_buffer.Read(c))
            {                
                count++;
            }

            return count;
        }

        int ConsumeMany(Func<int, bool> check)
        {
            int count = 0;

            while (check(_buffer.Peek()))
            {
                Read();
                count++;
            }

            return count;
        }

        bool ConsumeOne(char c)
        {
            return _buffer.Read(c);
        }

        bool ConsumeOne(Func<int, bool> check)
        {
            if (check(_buffer.Peek()))
            {
                Read();
                return true;
            }

            return false;
        }

        bool ConsumeOneEol()
        {
            var peek = _buffer.Peek();
            if (peek == '\n')
            {
                Read();
                return true;
            }
            
            if (peek == '\r' && _options.MultiEolns)
            {
                Read();
                if (_buffer.Peek() == '\n')
                    Read();
                
                return true;
            }

            return false;
        }

        #endregion

        #region Error reporting

        string Report(Severity severity, int errorCode, SourceSpan location, string message)
        {
            Debug.Assert(severity != Severity.FatalError);
            ErrorSink.Add(_sourceUnit, message, location, errorCode, severity);
            return message;
        }

        internal string ReportError(int errorCode, string format, params object[] args)
        {
            return Report(Severity.Error, errorCode, _buffer.TokenSpan, String.Format(format, args));
        }

        #endregion

        internal IEnumerable<Token> EnumerateTokens()
        {
            Token token;
            do
            {
                token = GetNextToken();
                
                yield return token;

            } while (token.Symbol != Symbol.Eof);            
        }

        internal static TokenInfo GetTokenInfo(Symbol token)
        {
            var result = new TokenInfo();

            // TODO: not finished

            switch (token)            
            {
                case Symbol.And:
                    result.Category = TokenCategory.Keyword;
                    break;

                case Symbol.Do:
                case Symbol.End:                
                    result.Category = TokenCategory.Keyword;
                    result.Trigger = TokenTriggers.MatchBraces;
                    break;

                case Symbol.Identifier:
                    result.Category = TokenCategory.Identifier;
                    break;

                case Symbol.Number:
                    result.Category = TokenCategory.NumericLiteral;
                    break;

                case Symbol.String:
                    result.Category = TokenCategory.StringLiteral;
                    break;

                case Symbol.Eof:
                    result.Category = TokenCategory.EndOfStream;
                    break;

                default:
                    throw Assert.Unreachable;
            }
            
            return result;
        }

        public override TokenInfo ReadToken()
        {
            var token = GetNextToken();
            TokenInfo result = GetTokenInfo(token.Symbol);
            result.SourceSpan = _lastTokenSpan;
            return result;
        }

        public SourceSpan CurrentTokenSpan() // DEBUG
        {
            return _buffer.TokenSpan;
        }

        public string CurrentTokenString() // DEBUG
        {
            return _buffer.GetTokenString();
        }

        public string CurrentTokenValue() // DEBUG
        {
            return _lastTokenValue ?? "<empty>";
        }
        
        public override SourceLocation CurrentPosition
        {
            get { return _buffer.TokenSpan.End; }
        }

        public override bool IsRestartable
        {
            get { return false; } // TODO: implement this feature
        }

        public override object CurrentState
        {
            get { return _state; }
        }

        public override ErrorSink ErrorSink
        {
            get { return _errors; }
            set
            {
                ContractUtils.RequiresNotNull(value, "value");
                _errors = value;
            }
        }

        public SourceUnit SourceUnit
        {
            get { return _sourceUnit; }
        }

        #region State

        public sealed class State : IEquatable<State>
        {
            public State(State other)
            {
            }

            public bool Equals(State other)
            {
                throw new NotImplementedException();
            }
        }

        #endregion

        #region Implementation of ILexer

        public LuaSyntaxException SyntaxException(string format, params object[] args)
        {
            var cpos = CurrentPosition;
            var file = _sourceUnit.HasPath ? _sourceUnit.Path : "<unknown>";
            return new LuaSyntaxException(file, cpos.Line, cpos.Column, String.Format(format, args));            
        }

        #endregion
    }

    static class HelperExtensions
    {
        public static bool IsIdentifierStart(this int c)
        {
            return ('a' <= c && c <= 'z') ||
                   ('A' <= c && c <= 'Z') ||
                   ('_' == c);
        }

        public static bool IsIdentifier(this int c)
        {
            return ('a' <= c && c <= 'z') ||
                   ('A' <= c && c <= 'Z') ||
                   ('0' <= c && c <= '9') ||
                   ('_' == c);
        }

        public static bool IsDecimal(this int c)
        {
            return ('0' <= c && c <= '9');
        }

        public static bool IsHex(this int c)
        {
            return ('0' <= c && c <= '9') ||
                   ('a' <= c && c <= 'f') ||
                   ('A' <= c && c <= 'F');
        }

        public static bool IsPunctuation(this int c)
        {
            switch (c)
            {
                case '+':
                case '-':
                case '*':
                case '/':
                case '%':
                case '^':
                case '#':
                case '~':
                case '<':
                case '>':
                case '=':
                case '(':
                case ')':
                case '{':
                case '}':
                case '[':
                case ']':
                case ';':
                case ':':
                case ',':
                case '.':
                    return true;
                default:
                    return false;
            }
        }

        public static bool IsWhitespace(this int c)
        {
            return (c == ' ' || c == '\t');
        }
    }

}