using System;
using IronLua.Runtime;
using Expr = System.Linq.Expressions.Expression;

namespace IronLua.Compiler
{
    static class ExprHelpers
    {
        public static Expr ConvertToBoolean(LuaContext context, Expr expression)
        {
            var convertBinder = context.CreateConvertBinder(typeof(bool), false);
            return Expr.Dynamic(convertBinder, typeof(bool), expression);
        }

        public static Expr ConvertToNumber(LuaContext context, Expr expression)
        {
            var convertBinder = context.CreateConvertBinder(typeof(double), false);
            return Expr.Dynamic(convertBinder, typeof(double), expression);
        }

        public static Expr CheckNumberForNan(LuaContext context, Expr number, string format, params object[] args)
        {
            return Expr.IfThen(
                Expr.Invoke(Expr.Constant((Func<double, bool>)Double.IsNaN), number),
                Expr.Throw(Expr.New(MemberInfos.NewRuntimeException, Expr.Constant(context), Expr.Constant(format), Expr.Constant(args))));
        }

        public static Expr ConvertToNumberAndCheck(LuaContext context, Expr expression, string format, params object[] args)
        {
            var numberVar = Expr.Variable(typeof(double));
            var assignNumber = Expr.Assign(numberVar, ConvertToNumber(context, expression));

            return Expr.Block(
                new[] {numberVar},
                assignNumber,
                Expr.Condition(
                    Expr.Invoke(
                        Expr.Constant((Func<double, bool>)Double.IsNaN), numberVar),
                    Expr.Block(
                        Expr.Throw(Expr.New(MemberInfos.NewRuntimeException, Expr.Constant(context), Expr.Constant(format), Expr.Constant(args))),
                        Expr.Constant(Double.NaN)),
                    numberVar));
        }
    }
}
