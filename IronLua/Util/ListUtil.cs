using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IronLua.Util
{
    static class ListUtil<T>
    {
        public static List<T> Create(int count, T item)
        {
            var list = new List<T>(count);
            for (int i = 0; i < count; i++)
                list[i] = item;
            return list;
        }

        public static List<T> Init(int count, Func<T> initalizer)
        {
            var list = new List<T>(count);
            for (int i = 0; i < count; i++)
                list.Add(initalizer());
            return list;
        }

        public static List<T> Init(int count, Func<int, T> initalizer)
        {
            var list = new List<T>(count);
            for (int i = 0; i < count; i++)
                list.Add(initalizer(i));
            return list;
        }
    }
}
