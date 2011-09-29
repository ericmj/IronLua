using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
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

        public static Varargs Assert(bool v, object message = null, params object[] additional)
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
                return CompileString(source)();
            }
            catch (LuaSyntaxException e)
            {
                throw new LuaRuntimeException(e.Message, e);
            }
        }

        public static void Error(string message, object level)
        {
            // TODO: Use level when call stacks are implemented
            throw new LuaRuntimeException(message);
        }

        public object GetFEnv(object f = null)
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

        public static Varargs IPairs(LuaTable t)
        {
            var length = t.Length();
            Func<double, object> func =
                index =>
                    {
                        index++;
                        return index > length ? null : new Varargs(index, t.GetValue(index));
                    };

            return new Varargs(func, t, 0.0);
        }

        public Varargs Load(Delegate func, string chunkname = "=(load)")
        {
            var invoker = Context.GetDynamicCall0();
            var sb = new StringBuilder(1024);

            while (true)
            {
                var result = invoker(func);
                if (result == null)
                    break;

                var str = result.ToString();
                if (String.IsNullOrEmpty(str))
                    break;

                sb.Append(str);
            }

            try
            {
                return new Varargs(CompileString(sb.ToString()));
            }
            catch (LuaSyntaxException e)
            {
                return new Varargs(null, e.Message);
            }
        }

        public Varargs LoadFile(string filename = null)
        {
            var source = filename == null ? Console.In.ReadToEnd() : File.ReadAllText(filename);
            try
            {
                return new Varargs(CompileString(source));
            }
            catch (LuaSyntaxException e)
            {
                return new Varargs(null, e.Message);
            }
        }

        public Varargs LoadString(string str, string chunkname = "=(loadstring)")
        {
            try
            {
                return new Varargs(CompileString(str));
            }
            catch (LuaSyntaxException e)
            {
                return new Varargs(null, e.Message);
            }
        }

        public static Varargs Next(LuaTable table, object index = null)
        {
            return table.Next(index);
        }

        public static Varargs Pairs(LuaTable t)
        {
            return new Varargs(t, (Func<LuaTable, object, Varargs>)Next, null);
        }

        public Varargs PCall(Delegate f, params object[] args)
        {
            try
            {
                var result = Context.GetDynamicCall1()(f, new Varargs(args));
                return new Varargs(true, result);
            }
            catch (LuaRuntimeException e)
            {
                return new Varargs(false, e.Message);
            }
        }

        public static void Print(params object[] args)
        {
            for (var i = 0; i < args.Length; i++)
            {
                if (i > 0)
                    Console.Out.Write("\t");
                Console.Out.Write(args[i]);
            }
            Console.Out.WriteLine();
        }

        public static bool RawEqual(object v1, object v2)
        {
            return v1.Equals(v2);
        }

        public static object RawGet(LuaTable table, object index)
        {
            return table.GetValue(index);
        }

        public static LuaTable RawSet(LuaTable table, object index, object value)
        {
            table.SetValue(index, value);
            return table;
        }

        public static Varargs Select(object index, params object[] args)
        {
            double num;
            string str;

            if ((str = index as string) != null)
            {
                if (str == "#")
                    return new Varargs(args.Length);
                num = InternalToNumber(str, 10.0);
                if (Double.IsNaN(num))
                    throw new LuaRuntimeException(ExceptionMessage.INVOKE_BAD_ARGUMENT_GOT, 1, "number", "string");
            }
            else if (index is double)
            {
                num = (double)index;
            }
            else
            {
                throw new LuaRuntimeException(ExceptionMessage.INVOKE_BAD_ARGUMENT_GOT, 1, "number",
                                              Type(index));
            }

            var numIndex = (int)Math.Round(num) - 1;
            if (numIndex >= args.Length || numIndex < 0)
                throw new LuaRuntimeException(ExceptionMessage.INVOKE_BAD_ARGUMENT, 1, "index out of range");

            var returnArgs = new object[args.Length - numIndex];
            Array.Copy(args, numIndex, returnArgs, 0, args.Length - numIndex);
            return new Varargs(returnArgs);
        }

        public object SetFEnv(object f, LuaTable table)
        {
            throw new LuaRuntimeException(ExceptionMessage.FUNCTION_NOT_IMPLEMENTED);
        }

        public static LuaTable SetMetatable(LuaTable table, LuaTable metatable)
        {
            if (table.Metatable != null && table.Metatable.GetValue(Constant.METATABLE_METAFIELD) != null)
                throw new LuaRuntimeException(ExceptionMessage.PROTECTED_METATABLE);

            table.Metatable = metatable;
            return table;
        }

        public static object ToNumber(object obj, object @base = null)
        {
            double numBase;
            string strBase;

            if (@base == null)
                numBase = 10.0;
            else if ((strBase = @base as string) != null)
                numBase = InternalToNumber(strBase, 10.0);
            else if (@base is double)
                numBase = (double)@base;
            else
                throw new LuaRuntimeException(ExceptionMessage.INVOKE_BAD_ARGUMENT_GOT, 2, "number", Type(@base));

            if (numBase == 10.0)
            {
                if (obj is double)
                    return obj;
            }
            else if (numBase < 2.0 || numBase > 36.0)
            {
                throw new LuaRuntimeException(ExceptionMessage.INVOKE_BAD_ARGUMENT, 2, "base out of range");
            }

            string stringStr;
            if ((stringStr = obj as string) == null)
                return null;

            var value = InternalToNumber(stringStr, numBase);
            return Double.IsNaN(value) ? null : (object)value;
        }

        public object ToString(object e)
        {
            // TODO: Fix casing of boolean's
            var metaToString = Context.GetMetamethod(e, Constant.TOSTRING_METAFIELD);
            if (metaToString == null)
                return e.ToString();

            return Context.GetDynamicCall1()(metaToString, e);
        }

        public static string Type(object v)
        {
            if (v == null)
                return "nil";
            if (v is bool)
                return "boolean";
            if (v is double)
                return "number";
            if (v is string)
                return "string";
            if (v is Delegate)
                return "function";
            if (v is LuaTable)
                return "table";

            return v.GetType().FullName;
        }

        public static Varargs Unpack(LuaTable list, object i = null, object j = null)
        {
            string tempString;
            double startIndex, length;
            var listLength = list.Length();

            if (i == null)
                startIndex = 1.0;
            else if ((tempString = i as string) != null)
                startIndex = Math.Round(InternalToNumber(tempString, 10.0));
            else if (i is double)
                startIndex = Math.Round((double)i);
            else
                throw new LuaRuntimeException(ExceptionMessage.INVOKE_BAD_ARGUMENT_GOT, 2, "number", Type(i));

            if (j == null)
                length = listLength;
            else if ((tempString = j as string) != null)
                length = Math.Round(InternalToNumber(tempString, 10.0));
            else if (j is double)
                length = Math.Round((double)j);
            else
                throw new LuaRuntimeException(ExceptionMessage.INVOKE_BAD_ARGUMENT_GOT, 3, "number", Type(j));

            if (startIndex < 1)
                return Varargs.Empty;
            length = Math.Min(length, listLength - startIndex + 1);

            var array = new object[(int)length];
            var arrayIndex = 0;
            for (var k = startIndex; k < startIndex + length; k++)
                array[arrayIndex++] = list.GetValue(k);

            return new Varargs(array);
        }

        public Varargs XPCall(Delegate f, Delegate err)
        {
            try
            {
                var result = Context.GetDynamicCall0()(f);
                return new Varargs(true, result);
            }
            catch (LuaRuntimeException e)
            {
                var result = Context.GetDynamicCall1()(err, e.Message);
                return new Varargs(false, result);
            }
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
                return NumberUtil.TryParseDecimalNumber(str, out result) ? result : Double.NaN;
            }

            if (intBase == 16)
            {
                if (str.StartsWith("0x") || str.StartsWith("0X"))
                {
                    if (NumberUtil.TryParseHexNumber(str.Substring(2), false, out result))
                        return result;
                }
                return Double.NaN;
            }

            var reversedStr = new String(str.Reverse().ToArray());
            for (var i = 0; i < reversedStr.Length; i++ )
            {
                var num = AlphaNumericToBase(reversedStr[i]);
                if (num == -1 || num >= intBase)
                    return Double.NaN;
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

        Func<object> CompileString(string source)
        {
            var input = new Input(source);
            var parser = new Parser(input);
            var ast = parser.Parse();
            var gen = new Generator(Context);
            var expr = gen.Compile(ast);
            return expr.Compile();
        }

        public override void Setup(LuaTable table)
        {
            table.SetValue("assert", (Func<bool, object, object[], Varargs>)Assert);
            table.SetValue("collectgarbage", (Action<string, string>)CollectGarbage);
            table.SetValue("dofile", (Func<string, object>)DoFile);
            table.SetValue("error", (Action<string, object>)Error);
            table.SetValue("_G", table);
            table.SetValue("getfenv", (Func<object, object>)GetFEnv);
            table.SetValue("getmetatable", (Func<object, object>)GetMetatable);
            table.SetValue("ipairs", (Func<LuaTable, Varargs>)IPairs);
            table.SetValue("load", (Func<Delegate, string, Varargs>)Load);
            table.SetValue("loadfile", (Func<string, Varargs>)LoadFile);
            table.SetValue("loadstring", (Func<string, string, Varargs>)LoadString);
            table.SetValue("next", (Func<LuaTable, object, Varargs>)Next);
            table.SetValue("pairs", (Func<LuaTable, Varargs>)Pairs);
            table.SetValue("pcall", (Func<Delegate, object[], Varargs>)PCall);
            table.SetValue("print", (Action<object[]>)Print);
            table.SetValue("rawequal", (Func<object, object, bool>)RawEqual);
            table.SetValue("rawget", (Func<LuaTable, object, object>)RawGet);
            table.SetValue("rawset", (Func<LuaTable, object, object, object>)RawSet);
            table.SetValue("select", (Func<object, object[], Varargs>)Select);
            table.SetValue("setfenv", (Func<object, LuaTable, object>)SetFEnv);
            table.SetValue("setmetatable", (Func<LuaTable, LuaTable, LuaTable>)SetMetatable);
            table.SetValue("tonumber", (Func<string, object, object>)ToNumber);
            table.SetValue("tostring", (Func<object, object>)ToString);
            table.SetValue("type", (Func<object, string>)Type);
            table.SetValue("unpack", (Func<LuaTable, object, object, Varargs>)Unpack);
            table.SetValue("_VERSION", Constant.LUA_VERSION);
            table.SetValue("xpcall", (Func<Delegate, Delegate, Varargs>)XPCall);
        }
    }
}
