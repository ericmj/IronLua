using System;

namespace IronLua.Library
{
    [AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
    sealed class InternalAttribute : Attribute
    {
    }
}
