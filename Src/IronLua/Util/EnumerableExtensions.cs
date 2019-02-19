using System;
using System.Collections.Generic;

namespace IronLua.Util
{
    static class EnumerableExtensions
    {
        public static IEnumerable<T> Add<T>(this IEnumerable<T> collection, T item)
        {
            foreach (var element in collection)
                yield return element;
            yield return item;
        }

        public static IEnumerable<T> Resize<T>(this IEnumerable<T> collection, int size, T item)
        {
            var i = 0;
            foreach (var element in collection)
            {
                if (i++ >= size)
                    yield break;
                yield return element;
            }

            for (; i < size; i++)
                yield return item;
        }

        public static IEnumerable<T> Resize<T>(this IEnumerable<T> collection, int size, Func<T> initalizer)
        {
            var i = 0;
            foreach (var element in collection)
            {
                if (i++ >= size)
                    yield break;
                yield return element;
            }

            for (; i < size; i++)
                yield return initalizer();
        }

        public static IEnumerable<T> Resize<T>(this IEnumerable<T> collection, int size, Func<int, T> initalizer)
        {
            var i = 0;
            foreach (var element in collection)
            {
                if (i++ >= size)
                    yield break;
                yield return element;
            }

            for (; i < size; i++)
                yield return initalizer(i);
        }

        public static IEnumerable<T> Resize<T>(this IEnumerable<T> collection, int size, IEnumerable<T> padder)
        {
            var i = 0;
            foreach (var element in collection)
            {
                if (i++ >= size)
                    yield break;
                yield return element;
            }

            foreach (var element in padder)
            {
                yield return element;
                i++;
            }

            if (i != size)
                throw new ArgumentException();
        }
    }
}
