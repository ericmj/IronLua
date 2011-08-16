using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IronLua.Util
{
    static class Extensions
    {
        public static IEnumerable<T> Add<T>(this IEnumerable<T> collection, T item)
        {
            foreach (var element in collection)
                yield return element;
            yield return item;
        }

        public static IEnumerable<T> Add<T>(this IEnumerable<T> collection, params T[] items)
        {
            foreach (var element in collection)
                yield return element;
            foreach (var item in items)
                yield return item;
        }
    }
}
