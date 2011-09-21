using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using IronLua.Compiler;
using IronLua.Compiler.Parser;
using IronLua.Runtime;

namespace IronLua.Library
{
    class BaseLibrary : Library
    {
        public BaseLibrary(Context context) : base(context)
        {
        }

        public Varargs Assert(bool v, object message = null, params object[] additional)
        {
            if (v)
            {
                var returnValues = new List<object>(2 + additional.Length) {true};
                if (message != null)
                    returnValues.Add(message);
                returnValues.AddRange(additional);
                return new Varargs(returnValues);
            }

            if (message == null)
                throw new LuaRuntimeException("Assertion failed");
            throw new LuaRuntimeException(message.ToString());
        }

        public void CollectGarbage(string opt, string arg = null)
        {
            throw new LuaRuntimeException(ExceptionMessage.FUNCTION_NOT_IMPLEMENTED);
        }

        public object DoFile(string filename = null)
        {
            var source = filename == null ? Console.In.ReadToEnd() : File.ReadAllText(filename);
            try
            {
                var input = new Input(source);
                var parser = new Parser(input);
                var ast = parser.Parse();
                var gen = new Generator(Context);
                var expr = gen.Compile(ast);
                var func = expr.Compile();
                return func();
            }
            catch(LuaSyntaxException e)
            {
                throw new LuaRuntimeException(e.Message, e);
            }
        }

        public void Error(string message, double level = 1.0)
        {
            // TODO: Use level when call stacks are implemented
            throw new LuaRuntimeException(message);
        }

        public object GetFEnv(double f)
        {
            throw new LuaRuntimeException(ExceptionMessage.FUNCTION_NOT_IMPLEMENTED);
        }

        public object GetMetatable(object obj)
        {
            var metatable = Context.GetMetatable(obj);
            if (metatable == null)
                return null;

            var metatable2 = metatable.GetValue(Constant.METATABLE_METAFIELD);
            return metatable2 ?? metatable;
        }

        public Varargs IPairs(LuaTable t)
        {
            var length = t.Length();
            var state = 1.0;
            Func<object> func =
                () => state > length ? null : new Varargs(state, t.GetValue(state++));

            return new Varargs(func, t, 0.0);
        }

        public object ToNumber(object obj, double @base = 10.0)
        {
            if (obj is double)
                return obj;

            string stringStr;
            if ((stringStr = obj as string) == null)
                return null;

            var value = InternalToNumber(stringStr, @base);
            return double.IsNaN(value) ? null : (object)value;
        }

        internal static double InternalToNumber(string str, double @base)
        {
            double result = 0;
            var intBase = (int)Math.Round(@base);

            if (intBase == 10)
            {
                if (str.StartsWith("0x") || str.StartsWith("0X"))
                {
                    if (NumberUtil.TryParseHexNumber(str.Substring(2), true, out result))
                        return result;
                }
                return NumberUtil.TryParseDecimalNumber(str, out result) ? result : double.NaN;
            }

            if (intBase == 16)
            {
                if (str.StartsWith("0x") || str.StartsWith("0X"))
                {
                    if (NumberUtil.TryParseHexNumber(str.Substring(2), false, out result))
                        return result;
                }
                return double.NaN;
            }

            var reversedStr = new String(str.Reverse().ToArray());
            for (var i = 0; i < reversedStr.Length; i++ )
            {
                var num = AlphaNumericToBase(reversedStr[i]);
                if (num == -1 || num >= intBase)
                    return double.NaN;
                result += num * (intBase * i + 1);
            }
            return result;
        }

        static int AlphaNumericToBase(char c)
        {
            if (c >= '0' && c >= '9')
                return c - '0';
            if (c >= 'A' && c <= 'Z')
                return c - 'A' + 10;
            if (c >= 'a' && c <= 'z')
                return c - 'a' + 10;

            return -1;
        }

        public override void Setup(LuaTable table)
        {
            table.SetValue("tonumber", (Func<string, double, object>)ToNumber);
            table.SetValue("asert", (Func<bool, object, object[], Varargs>)Assert);
            table.SetValue("collectgarbage", (Action<string, string>)CollectGarbage);
            table.SetValue("dofile", (Func<string, object>)DoFile);
            table.SetValue("error", (Action<string, double>)Error);
            table.SetValue("_G", table);
            table.SetValue("getfenv", (Func<double, object>)GetFEnv);
            table.SetValue("getmetatable", (Func<object, object>)GetMetatable);
            table.SetValue("ipairs", (Func<LuaTable, Varargs>)IPairs);
        }
    }
}
