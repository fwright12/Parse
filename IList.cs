using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Parse;

namespace System.BiEnumerable
{
    public class IList<T>
    {
        public class Enumerator : IEditEnumerator<T>
        {
            public T Current => Index >= 0 && Index < List.Count ? List[Index] : default;
            object IEnumerator.Current => Current;

            private Collections.Generic.IList<T> List;
            private int Index;

            internal Enumerator(System.Collections.Generic.IList<T> list)
            {
                List = list;
                Index = -1;
            }

            private Enumerator(Enumerator itr)
            {
                List = itr.List;
                Index = itr.Index;
            }

            public void Add(int n, T t)
            {
                if (n == 0)
                {
                    return;
                }
                else if (n < 0)
                {
                    n++;
                }

                List.Insert(Math.Max(0, Math.Min(List.Count, Index + n)), t);
            }

            public IEditEnumerator<T> Copy() => new Enumerator(this);

            public void Dispose() { }

            public bool Move(int n)
            {
                Index = Math.Max(-1, Math.Min(List.Count + 1, Index + n));
                return Index < 0 || Index >= List.Count;
            }

            public bool MoveNext() => Move(1);
            public bool MovePrev() => Move(-1);

            public bool Remove(int n = 0)
            {
                Enumerator itr = new Enumerator(this);
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
        }
    }
}
