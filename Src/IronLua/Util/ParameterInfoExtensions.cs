using System;
using System.Reflection;

namespace IronLua.Util
{
    static class ParameterInfoExtensions
    {
        public static bool IsParams(this ParameterInfo parameter)
        {
            return Attribute.IsDefined(parameter, typeof(ParamArrayAttribute));
        }
    }
}
