using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Dynamic;
using Microsoft.Scripting.Actions;
using Microsoft.Scripting.Actions.Calls;
using Microsoft.Scripting.Runtime;
using IronLua.Library;

namespace IronLua.Runtime.Binder
{
    class LuaBinder : DefaultBinder
    {
        private readonly LuaContext _context;

        public LuaBinder(LuaContext context)
        {
            Contract.Requires(context != null);
            _context = context;
        }

        public LuaBinder(LuaBinder binder)
        {
            _context = binder._context;
        }

        public override bool CanConvertFrom(System.Type fromType, System.Type toType, bool toNotNullable, NarrowingLevel level)
        {
            if (fromType == typeof(double) && toType == typeof(string))
                return true;
            else if (fromType == typeof(string) && toType == typeof(double))
                return !toNotNullable;
            
            return base.CanConvertFrom(fromType, toType, toNotNullable, level);
        }

        public override object Convert(object obj, System.Type toType)
        {
            if (obj is double && toType == typeof(string))
                return BaseLibrary.ToStringEx(obj);

            else if (obj is string && toType == typeof(double))
                return BaseLibrary.ToNumber(obj, 10);

            return base.Convert(obj, toType);
        }
    }

    internal sealed class LuaOverloadResolverFactory : OverloadResolverFactory
    {
        private readonly LuaBinder _binder;

        public LuaOverloadResolverFactory(LuaBinder binder)
        {
            Contract.Requires(binder != null);
            _binder = binder;
        }

        public override DefaultOverloadResolver CreateOverloadResolver(IList<DynamicMetaObject> args, CallSignature signature, CallTypes callType)
        {
            return new LuaOverloadResolver(_binder, args, signature, callType);
        }
    }

    internal sealed class LuaOverloadResolver : DefaultOverloadResolver
    {
        public LuaOverloadResolver(ActionBinder binder, DynamicMetaObject instance, IList<DynamicMetaObject> args, CallSignature signature) 
            : base(binder, instance, args, signature)
        {
        }

        public LuaOverloadResolver(ActionBinder binder, IList<DynamicMetaObject> args, CallSignature signature) 
            : base(binder, args, signature)
        {
        }

        public LuaOverloadResolver(ActionBinder binder, IList<DynamicMetaObject> args, CallSignature signature, CallTypes callType) 
            : base(binder, args, signature, callType)
        {
        }

        private new LuaBinder Binder
        {
            get { return (LuaBinder)base.Binder; }
        }
    }
}