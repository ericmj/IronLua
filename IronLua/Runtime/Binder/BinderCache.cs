using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace IronLua.Runtime.Binder
{
    class BinderCache
    {
        Dictionary<ExpressionType, BinaryOperationBinder> binaryOperationBinders;

        public BinderCache()
        {
            binaryOperationBinders = new Dictionary<ExpressionType, BinaryOperationBinder>();
        }

        public BinaryOperationBinder GetBinaryOperationBinder(ExpressionType op)
        {
            BinaryOperationBinder binder;
            if (binaryOperationBinders.TryGetValue(op, out binder))
                return binder;

            binder = new LuaBinaryOperationBinder(op);
            binaryOperationBinders[op] = binder;
            return binder;
        }
    }
}
