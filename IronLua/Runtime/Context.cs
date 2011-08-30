using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using IronLua.Library;
using IronLua.Runtime.Binder;
using Expr = System.Linq.Expressions.Expression;
using ExprType = System.Linq.Expressions.ExpressionType;
using ParamExpr = System.Linq.Expressions.ParameterExpression;

namespace IronLua.Runtime
{
    class Context
    {
        public LuaTable Globals { get; private set; }
        public BinderCache BinderCache { get; private set; }
        public Dictionary<Type, LuaTable> Metatables { get; private set; }

        internal Global GlobalsLibrary;
        internal LuaString StringLibrary;

        public Context()
        {
            BinderCache = new BinderCache(this);

            SetupLibraries();

            Metatables =
                new Dictionary<Type, LuaTable>
                    {
                        {typeof(bool), new LuaTable()},
                        {typeof(double), new LuaTable()},
                        {typeof(string), new LuaTable()},
                        {typeof(LuaFunction), new LuaTable()}
                    };
        }

        void SetupLibraries()
        {
            GlobalsLibrary = new Global(this);
            StringLibrary = new LuaString(this);

            Globals = new LuaTable();
            GlobalsLibrary.Setup(Globals);
            //StringLibrary.Setup(StringGlobals);
        }

        internal object ConcatMetamethod(object left, object right)
        {
            dynamic metamethod = GetMetamethod(Constant.CONCAT_METAMETHOD, left) ??
                                 GetMetamethod(Constant.CONCAT_METAMETHOD, right);
            if (metamethod == null)
                throw new Exception(); // TODO
            return metamethod(left, right);
        }

        internal object BinaryOpMetamethod(ExprType op, object left, object right)
        {
            switch (op)
            {
                case ExprType.Add:
                case ExprType.Subtract:
                case ExprType.Multiply:
                case ExprType.Divide:
                case ExprType.Modulo:
                case ExprType.Power:
                    return NumericMetamethod(op, left, right);

                case ExprType.GreaterThan:
                case ExprType.GreaterThanOrEqual:
                case ExprType.LessThan:
                case ExprType.LessThanOrEqual:
                    return RelationalMetamethod(op, left, right);

                default:
                    throw new Exception(); // TODO
            }
        }
        
        object NumericMetamethod(ExprType op, object left, object right)
        {
            var methodName = GetMethodName(op);

            dynamic metamethod = GetMetamethod(methodName, left) ?? GetMetamethod(methodName, right);
            if (metamethod == null)
                throw new Exception(); // TODO
            return metamethod(left, right);
        }

        object RelationalMetamethod(ExprType op, object left, object right)
        {
            if (left.GetType() != right.GetType())
                return false;

            // There are no metamethods for 'a > b' and 'a >= b' so they are translated to 'b < a' and 'b <= a' respectively
            bool invert = op == ExprType.GreaterThan || op == ExprType.GreaterThanOrEqual;

            dynamic metamethod = GetRelationalMetamethod(op, left, right);

            if (metamethod == null)
            {
                // In the absence of a '<=' metamethod, try '<', 'a <= b' is translated to 'not (b < a)'
                if (op != ExprType.LessThanOrEqual && op != ExprType.GreaterThanOrEqual)
                    return false;

                metamethod = GetRelationalMetamethod(ExprType.LessThan, left, right);
                if (metamethod == null)
                    return false;

                return invert ? Global.Not(metamethod(right, left)) : Global.Not(metamethod(left, right));
            }

            return invert ? metamethod(right, left) : metamethod(left, right); ;
        }

        dynamic GetRelationalMetamethod(ExprType op, object left, object right)
        {
            var methodName = GetMethodName(op);
            dynamic metamethodLeft = GetMetamethod(methodName, left);
            dynamic metamethodRight = GetMetamethod(methodName, right);
            return metamethodLeft != metamethodRight ? null : metamethodLeft;
        }

        string GetMethodName(ExprType op)
        {
            string methodName;
            if (!Constant.METAMETHODS.TryGetValue(op, out methodName))
                throw new Exception(); // TODO
            return methodName;
        }

        object GetMetamethod(string methodName, object obj)
        {
            LuaTable metatable;
            if (!Metatables.TryGetValue(obj.GetType(), out metatable))
                throw new Exception(); // TODO

            return metatable.GetValue(methodName);
        }
    }
}
