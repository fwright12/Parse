using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Parse.Collections.Generic
{
    internal class ListEnumerator<T> : IEditEnumerator<T>
    {
        public T Current => Index >= 0 && Index < List.Count ? List[Index] : default;
        object IEnumerator.Current => Current;

        private IList<T> List;
        private int Index;

        internal ListEnumerator(IList<T> list)
        {
            List = list;
            Index = -1;
        }

        private ListEnumerator(ListEnumerator<T> itr)
        {
            List = itr.List;
            Index = itr.Index;
        }

        public void Add(int n, T t)
        {
            if (n == 0)
            {
                if (Index >= 0 && Index < List.Count)
                {
                    List[Index] = t;
                }
                return;
            }
            else if (n < 0)
            {
                n++;
            }
            
            List.Insert(Math.Max(0, Math.Min(List.Count, Index + n)), t);
        }

        public IEditEnumerator<T> Copy() => new ListEnumerator<T>(this);
        IEditEnumerator IEditEnumerator.Copy() => Copy();

        public void Dispose() { }

        public bool Move(int n)
        {
            Index = Math.Max(-1, Math.Min(List.Count + 1, Index + n));
            return Index < 0 || Index >= List.Count;
        }

        public bool MoveNext() => Move(1);
        public bool MovePrev() => Move(-1);

        public bool Remove(int n)
        {
            ListEnumerator<T> itr = new ListEnumerator<T>(this);
            if (!itr.Move(n))
            {
                return false;
            }

            List.RemoveAt(itr.Index);
            return true;
        }

        public void Reset()
        {
            Index = -1;
        }

        public override bool Equals(object obj) => obj is ListEnumerator<T> && Index == ((ListEnumerator<T>)obj).Index;
    }
}