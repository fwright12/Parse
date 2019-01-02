using System;
using System.Collections.Generic;
using System.Text;

namespace Crunch.Machine
{
    public class Quantity
    {
        public Node<object> First => first;
        public Node<object> Last => last;

        protected Node<object> first = null;
        protected Node<object> last = null;

        public Quantity(params object[] list)
        {
            foreach(object o in list)
            {
                AddLast(o);
            }
        }

        private Quantity(Node<object> first, Node<object> last)
        {
            if (first == null || last == null)
            {
                this.first = this.last = null;
            }
            else
            {
                first.Previous = null;
                this.first = first;
                last.Next = null;
                this.last = last;
            }
        }

        public void AddFirst(object o) => AddFirst(new Node<object>(o));
        public void AddFirst(Node<object> node)
        {
            if (first == null)
            {
                first = last = node;
            }
            else
            {
                first.Previous = node;
                node.Next = first;
                first = node;
            }
        }

        public void AddLast(object o) => AddLast(new Node<object>(o));
        public void AddLast(Node<object> node)
        {
            if (last == null)
            {
                first = last = node;
            }
            else
            {
                last.Next = node;
                node.Previous = last;
                last = node;
            }
        }

        public void Replace(Node<object> node, Node<object> replacement) => Replace(node, node, replacement);

        /// <summary>
        /// Replace the nodes from start to end with replacement
        /// </summary>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <param name="replacement"></param>
        public void Replace(Node<object> start, Node<object> end, Node<object> replacement)
        {
            Node<object> before = start?.Previous;
            //Splice(start?.Previous, replacement);
            //Remove(start?.Previous, end?.Next);

            Remove(start?.Previous, end?.Next);
            Splice(before, replacement);
        }

        /// <summary>
        /// Splice in node after pos
        /// </summary>
        /// <param name="pos"></param>
        /// <param name="node"></param>
        public void Splice(Node<object> pos, Node<object> node) => Splice(pos, node, node);

        /// <summary>
        /// Splice in the nodes from start to end after pos
        /// </summary>
        /// <param name="pos"></param>
        /// <param name="start"></param>
        /// <param name="end"></param>
        public void Splice(Node<object> pos, Node<object> start, Node<object> end)
        {
            Node<object> endPos = (pos == null) ? first : pos.Next;

            //Splice at beginning
            if (pos == null)
            {
                first = start;
            }
            else
            {
                pos.Next = start;
            }
            start.Previous = pos;

            //Splice at end
            if (endPos == null)
            {
                last = end;
            }
            else
            {
                endPos.Previous = end;
            }
            end.Next = endPos;
        }

        public Node<object> Remove(Node<object> node)
        {
            if (node != null)
            {
                Remove(node?.Previous, node?.Next);
            }
            return node;
        }

        /// <summary>
        /// Remove everything between start and end
        /// </summary>
        /// <param name="start"></param>
        /// <param name="end"></param>
        public void Remove(Node<object> start, Node<object> end)
        {
            if (start == null)
            {
                first = end;
            }
            else
            {
                start.Next = end;
            }

            if (end == null)
            {
                last = start;
            }
            else
            {
                end.Previous = start;
            }
        }

        bool forward = true;
        public override string ToString()
        {
            string s = "";
            Node<object> temp = forward ? first : last;
            while (temp != null)
            {
                s += temp.Value?.ToString() ?? "null";
                temp = forward ? temp.Next : temp.Previous;
            }

            return "[" + s + "]";
        }
    }
}
