using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IronLua.Runtime
{
    class Varargs : IList, IList<object>
    {
        readonly object[] data;

        private Varargs()
        {
        }

        public Varargs(object[] data)
        {
            this.data = data;
        }

        public Varargs(IEnumerable<object> data)
        {
            this.data = data.ToArray();
        }

        public int Count { get { return data.Length; } }

        public object First()
        {
            return data.Length > 0 ? data[0] : null;
        }

        public IEnumerator<object> GetEnumerator()
        {
            return (IEnumerator<object>)data.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return data.GetEnumerator();
        }

        public void CopyTo(Array array, int index)
        {
            data.CopyTo(array, index);
        }

        bool ICollection<object>.Remove(object item)
        {
            throw new InvalidOperationException("Varargs is readonly");
        }

        int ICollection<object>.Count
        {
            get { return data.Length; }
        }

        bool ICollection<object>.IsReadOnly
        {
            get { return true; }
        }

        int ICollection.Count
        {
            get { return data.Length; }
        }

        public object SyncRoot
        {
            get { return this; }
        }

        public bool IsSynchronized
        {
            get { return false; }
        }

        public int Add(object value)
        {
            throw new InvalidOperationException("Varargs is readonly");
        }

        void ICollection<object>.Clear()
        {
            throw new InvalidOperationException("Varargs is readonly");
        }

        bool ICollection<object>.Contains(object item)
        {
            return data.Contains(item);
        }

        public void CopyTo(object[] array, int arrayIndex)
        {
            data.CopyTo(array, arrayIndex);
        }

        public bool Contains(object value)
        {
            return data.Contains(value);
        }

        void ICollection<object>.Add(object item)
        {
            throw new InvalidOperationException("Varargs is readonly");
        }

        void IList.Clear()
        {
            throw new InvalidOperationException("Varargs is readonly");
        }

        public int IndexOf(object value)
        {
            for (int i=0; i<data.Length; i++)
            {
                if (data[i] == value)
                    return i;
            }
            return -1;
        }

        void IList<object>.Insert(int index, object item)
        {
            throw new InvalidOperationException("Varargs is readonly");
        }

        void IList<object>.RemoveAt(int index)
        {
            throw new InvalidOperationException("Varargs is readonly");
        }

        public object this[int index]
        {
            get { return data[index]; }
            set { throw new InvalidOperationException("Varargs is readonly"); }
        }

        int IList<object>.IndexOf(object item)
        {
            return IndexOf(item);
        }

        public void Insert(int index, object value)
        {
            throw new InvalidOperationException("Varargs is readonly");
        }

        public void Remove(object value)
        {
            throw new InvalidOperationException("Varargs is readonly");
        }

        void IList.RemoveAt(int index)
        {
            throw new InvalidOperationException("Varargs is readonly");
        }

        object IList.this[int index]
        {
            get { return data[index]; }
            set { throw new InvalidOperationException("Varargs is readonly"); }
        }

        bool IList.IsReadOnly
        {
            get { return true; }
        }

        public bool IsFixedSize
        {
            get { return true; }
        }
    }
}
