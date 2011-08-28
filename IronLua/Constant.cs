using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using ExprType = System.Linq.Expressions.ExpressionType;

namespace IronLua
{
    static class Constant
    {
        public static readonly CultureInfo INVARIANT_CULTURE = CultureInfo.InvariantCulture;

        // Only includes metamethods that can be translated from ExprTypes
        public static readonly Dictionary<ExprType, string> METAMETHODS =
            new Dictionary<ExprType, string>
                {
                    {ExprType.Equal,              "__eq"},
                    {ExprType.NotEqual,           "__eq"},
                    {ExprType.LessThan,           "__lt"},
                    {ExprType.GreaterThan,        "__gt"},
                    {ExprType.LessThanOrEqual,    "__le"},
                    {ExprType.GreaterThanOrEqual, "__gt"},
                    {ExprType.Add,                "__add"},
                    {ExprType.Subtract,           "__sub"},
                    {ExprType.Multiply,           "__mul"},
                    {ExprType.Divide,             "__div"},
                    {ExprType.Modulo,             "__mod"},
                    {ExprType.Power,              "__pow"},
                };
    }
}
