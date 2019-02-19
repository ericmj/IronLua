using System.Collections.Generic;
using ExprType = System.Linq.Expressions.ExpressionType;

namespace IronLua
{
    static class Constant
    {
        // Only includes metamethods that can be translated from ExprTypes
        public static readonly Dictionary<ExprType, string> METAMETHODS =
            new Dictionary<ExprType, string>
                {
                    {ExprType.NotEqual,           "__eq"},
                    {ExprType.GreaterThan,        "__lt"},
                    {ExprType.GreaterThanOrEqual, "__le"},

                    {ExprType.DebugInfo,          "__doc"},

                    {ExprType.Equal,              "__eq"},
                    {ExprType.LessThan,           "__lt"},
                    {ExprType.LessThanOrEqual,    "__le"},
                    {ExprType.Add,                "__add"},
                    {ExprType.Subtract,           "__sub"},
                    {ExprType.Multiply,           "__mul"},
                    {ExprType.Divide,             "__div"},
                    {ExprType.Modulo,             "__mod"},
                    {ExprType.Power,              "__pow"}
                };

        public const string LUA_VERSION = "Lua 5.1";

        public const string CONCAT_METAMETHOD = "__concat";
        public const string LENGTH_METAMETHOD = "__len";
        public const string UNARYMINUS_METAMETHOD = "__unm";
        public const string INDEX_METAMETHOD = "__index";
        public const string NEWINDEX_METAMETHOD = "__newindex";
        public const string CALL_METAMETHOD = "__call";
        public const string METATABLE_METAFIELD = "__metatable";
        public const string TOSTRING_METAFIELD = "__tostring";

        /// <summary>
        /// Non-standard metafield providing inline documentation access for tables/types
        /// </summary>
        public const string DOC_METAFIELD = "__doc";

        public const string VARARGS = "$varargs$";
        public const string FUNCTION_PREFIX = "lua$";
    }
}
