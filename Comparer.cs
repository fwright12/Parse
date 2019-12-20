using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Parse
{
    public class Comparer
    {
        public readonly Func<object, bool> Compare;

        public Comparer(Func<object, bool> compare)
        {
            Compare = compare;
        }

        public static implicit operator Comparer(Func<object, bool> compare) => new Comparer(compare);
        public static implicit operator Comparer(string str) => new Comparer((o) => o.ToString() == str);
        public static implicit operator Comparer(Type type) => new Comparer((o) => o.GetType() == type);

        public Comparer AND(Comparer comparer) => new Comparer((o) => Compare(o) && comparer.Compare(o));
        public Comparer OR(Comparer comparer) => new Comparer((o) => Compare(o) || comparer.Compare(o));

        //public static bool operator true(Comparer comparer) => false;
        //public static bool operator false(Comparer comparer) => false;

        public static Comparer operator !(Comparer comparer) => new Comparer((o) => !comparer.Compare(o));
        public static Comparer operator &(Comparer comparer1, Comparer comparer2) => new Comparer((o) => comparer1.Compare(o) & comparer2.Compare(o));
        public static Comparer operator |(Comparer comparer1, Comparer comparer2) => new Comparer((o) => comparer1.Compare(o) | comparer2.Compare(o));
        public static Comparer operator ^(Comparer comparer1, Comparer comparer2) => new Comparer((o) => comparer1.Compare(o) ^ comparer2.Compare(o));
    }
}
