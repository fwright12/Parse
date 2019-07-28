using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Parse;

namespace System.BiEnumerable
{
    public class LinkedList<T> : Collections.Generic.LinkedList<T>, IOrdered<T>
    {
        new public IEditEnumerator<T> GetEnumerator() => new Enumerator(this);
        IBiEnumerator<T> IBiEnumerable<T>.GetEnumerator() => new Enumerator(this);
        IEditEnumerator<T> IEditEnumerable<T>.GetEnumerator() => GetEnumerator();

        [Serializable]
        new public struct Enumerator : IEditEnumerator<T>
        {
            public T Current => Node == null ? default : Node.Value;
            object IEnumerator.Current => throw new NotImplementedException();

            private System.Collections.Generic.LinkedList<T> List;
            private LinkedListNode<T> Node;
            private bool begin;

            internal Enumerator(System.Collections.Generic.LinkedList<T> list)
            {
                List = list;
                Node = null;
                begin = true;
            }

            internal Enumerator(LinkedListNode<T> node) : this(node.List)
            {
                Node = node;
            }

            private Enumerator(Enumerator itr)
            {
                List = itr.List;
                Node = itr.Node;
                begin = itr.begin;
            }
            
            public void Add(int n, T t)
            {
                if (n == 0)
                {
                    if (Node != null)
                    {
                        Node.Value = t;
                    }
                    return;
                }

                Enumerator itr = new Enumerator(this);
                if (!itr.Move(n - Math.Sign(n)))
                {
                    if (begin)
                    {
                        List.AddFirst(t);
                    }
                    else
                    {
                        List.AddLast(t);
                    }

                    /*if (!itr.Move(-Math.Sign(n)))
                    {
                        itr.Move(Math.Sign(n));
                        n *= -1;
                    }*/
                }
                else
                {
                    if (n > 0)
                    {
                        List.AddAfter(itr.Node, t);
                    }
                    else if (n < 0)
                    {
                        List.AddBefore(itr.Node, t);
                    }
                }
            }

            public IEditEnumerator<T> Copy() => new Enumerator(this);

            public void Dispose() { }

            public bool Move(int n)
            {
                for (int i = 0; i < Math.Abs(n); i++)
                {
                    if (n > 0)
                    {
                        Node = Node == null && begin ? List.First : Node?.Next;
                        begin = false;
                    }
                    else if (n < 0)
                    {
                        Node = Node == null && !begin ? List.Last : Node?.Previous;
                        begin = true;
                    }

                    if (Node == null)
                    {
                        return false;
                    }
                }

                return !(n == 0 && Node == null);
            }

            public bool MoveNext() => Move(1);
            public bool MovePrev() => Move(-1);

            public bool Remove(int n)
            {
                Enumerator itr = new Enumerator(this);
                if (!itr.Move(n))
                {
                    return false;
                }

                List.Remove(itr.Node);
                return true;
            }

            public void Reset()
            {
                Node = null;
                begin = true;
            }

            public override bool Equals(object obj)
            {
                return obj is Enumerator && Node == ((Enumerator)obj).Node;
            }
        }
    }
}
