using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

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
