using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace IronLua.Runtime
{
    internal class Varargs : ReadOnlyCollection<object>
    {
        public static readonly Varargs Empty = new Varargs();

        public Varargs(params object[] data)
            : base(data)
        {
        }

        public Varargs(IEnumerable<object> data)
            : base(data.ToArray())
        {            
        }

        public object First()
        {
            return Items.FirstOrDefault();
        }
    }
}
