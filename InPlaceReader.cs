using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Extensions;

#if DEBUG
namespace Parse
{
    public abstract class Reader<T>
    {
        public int Count => dict.Count;

        readonly protected IDictionary<T, Operator<T>> Operations;

        public HashSet<T> Opening;
        public HashSet<T> Closing;
        public HashSet<T> Ignore;

        private Dictionary<T, int> dict;

        public Reader(params KeyValuePair<T, Operator<T>>[][] data) : this(new Dictionary<T, Operator<T>>()) { }

        protected Reader(IDictionary<T, Operator<T>> operations, params KeyValuePair<T, Operator<T>>[][] data)
        {
            Operations = operations;
            dict = new Dictionary<T, int>();

            Opening = new HashSet<T>();
            Closing = new HashSet<T>();
            Ignore = new HashSet<T>();

            for (int i = 0; i < data.Length; i++)
            {
                foreach (KeyValuePair<T, Operator<T>> kvp in data[i])
                {
                    if (Opening.Contains(kvp.Key) || Closing.Contains(kvp.Key))
                    {
                        throw new Exception("The character " + kvp + " cannot appear in a command - this character is used to separate quantities");
                    }

                    Insert(i, kvp.Key, kvp.Value);
                }
            }
        }

        public void Add(T symbol, Operator<T> operation) => Insert(dict.Count, symbol, operation);

        public void Insert(int index, T symbol, Operator<T> operation)
        {
            KeyValuePair<T, Operator<T>> pair = new KeyValuePair<T, Operator<T>>(symbol, operation);
            dict.Add(pair.Key, index);
            Operations.Add(pair.Key, pair.Value);
        }

        private int IndexOf(T key) => dict.ContainsKey(key) ? dict[key] : -1;

        protected abstract IEnumerable<T> ParseOperand(T operand);

        protected virtual T Juxtapose(IEditEnumerable<object> expression) => throw new Exception();

        public T Parse(IEnumerable<T> input)
        {
            string parsing = "parsing |";
            foreach (T s in input)
            {
                parsing += s + "|";
            }
            Print.Log(parsing);

            Stack<Evaluator> quantities = new Stack<Evaluator>();

            IEnumerator<T> itr = input.GetEnumerator();
            quantities.Push(new Evaluator(new System.BiEnumerable.LinkedList<object>(), new LinkedList<LinkedListNode<object>>[Count]));

            while (true)
            {
                Evaluator quantity = quantities.Peek();

                bool done = !itr.MoveNext();

                if (done || Closing.Contains(itr.Current))
                {
                    Evaluator e = quantities.Pop();
                    Print.Log("close", e.Input.Count);
                    LinkedListNode<object> a = e.Input.First;
                    while (a != null)
                    {
                        Print.Log(a.Value, a.Value?.GetType());
                        a = a.Next;
                    }
                    Print.Log("\n");

                    T answer = Close(e);

                    if (quantities.Count == 0)
                    {
                        if (done)
                        {
                            return answer;
                        }
                        else
                        {
                            System.BiEnumerable.LinkedList<object> front = new System.BiEnumerable.LinkedList<object>();
                            front.AddFirst(answer);
                            quantities.Push(new Evaluator(front, new LinkedList<LinkedListNode<object>>[Count]));
                        }
                    }
                    else
                    {
                        quantities.Peek().Input.AddLast(answer);
                    }
                }
                else if (Opening.Contains(itr.Current))
                {
                    Print.Log("open");
                    quantities.Push(new Evaluator(new System.BiEnumerable.LinkedList<object>(), new LinkedList<LinkedListNode<object>>[Count]));
                }
                else
                {
                    T next = itr.Current;

                    Operator<T> operation;
                    if (Operations.TryGetValue(next, out operation))
                    {
                        Print.Log("found operator", next);

                        int index = IndexOf(next);

                        // Put the operator in the linked list as a node
                        LinkedListNode<object> node = new LinkedListNode<object>(operation);

                        // Get the list of all of this type of operator (e.g. all instances of "+")
                        if (quantity.Operations[index] == null)
                        {
                            quantity.Operations[index] = new LinkedList<LinkedListNode<object>>();
                        }
                        LinkedList<LinkedListNode<object>> list = quantity.Operations[index];

                        if (operation.Order == ProcessingOrder.RightToLeft)
                        {
                            list.AddFirst(node);
                        }
                        else
                        {
                            list.AddLast(node);
                        }

                        quantity.Input.AddLast(node);
                    }
                    else
                    {
                        Print.Log("found operand", next);

                        foreach (T o in ParseOperand(next))
                        {
                            Print.Log("\t" + o);
                            quantity.Input.AddLast(o);
                        }
                    }
                }
            }
        }

        /*private IEnumerable<TInput> Next(IEnumerable<TInput> input)
        {
            foreach(TInput t in input)
            {
                if (!Ignore.Contains(t))
                {
                    yield return t;
                }
            }
        }*/

        private T Close(Evaluator quantity)
        {
            for (int j = 0; j < quantity.Operations.Length; j++)
            {
                LinkedList<LinkedListNode<object>> stack = quantity.Operations[j];

                while (stack?.Count > 0)
                {
                    LinkedListNode<object> node = stack.Dequeue().Value;
                    if (node.List == null)
                    {
                        continue;
                    }
                    Operator<T> op = (Operator<T>)node.Value;

                    IEditEnumerator<object>[] operandNodes = new IEditEnumerator<object>[op.Targets.Length];

                    for (int k = 0; k < op.Targets.Length; k++)
                    {
                        operandNodes[k] = new System.BiEnumerable.LinkedList<object>.Enumerator(node);
                        op.Targets[k](operandNodes[k]);
                    }

                    T[] operands = new T[operandNodes.Length];
                    for (int k = 0; k < operands.Length; k++)
                    {
                        operands[k] = (T)operandNodes[k].Current;
                        operandNodes[k].Remove(0);
                    }

                    Print.Log("operating", op.GetType(), operands.Length);
                    foreach (object o in operands)
                    {
                        Print.Log(o, o.GetType());
                    }
                    Print.Log("done");

                    node.Value = op.Operate(operands);
                }
            }

            if (quantity.Input.Count == 0)
            {
                throw new Exception();
            }

            //IOrdered<object> itr = new LinkedListBiEnumerator<object>(input);
            //itr.MoveNext();
            //Other.LinkedList<object> list = input;
            return Juxtapose(quantity.Input);
        }

        private class Evaluator
        {
            public System.BiEnumerable.LinkedList<object> Input;
            public LinkedList<LinkedListNode<object>>[] Operations;

            public Evaluator(System.BiEnumerable.LinkedList<object> input, LinkedList<LinkedListNode<object>>[] operations)
            {
                Input = input;
                Operations = operations;
            }
        }
    }
}
#endif