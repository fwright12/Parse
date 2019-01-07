using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Crunch.Machine
{
    public class Node<T>
    {
        public T Value;
        public Node<T> Next = null;
        public Node<T> Previous = null;

        public static Func<Node<T>, Node<T>> NextNode = (node) => node.Next;
        public static Func<Node<T>, Node<T>> PreviousNode = (node) => node.Previous;

        public Node(T value)
        {
            Value = value;
        }

        public void Delete()
        {
            
        }

        public static Node<T> operator +(Node<T> node, int i) => iterate(i < 0 ? PreviousNode : NextNode, node, i);

        private static Node<T> iterate(Func<Node<T>, Node<T>> iterator, Node<T> node, int iterations)
        {
            for (int i = 0; i < Math.Abs(iterations); i++)
            {
                node = iterator(node);
            }
            return node;
        }
    }
}
