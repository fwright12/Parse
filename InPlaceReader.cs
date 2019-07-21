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
            T quantity = default;

            IEnumerator<T> itr = input.GetEnumerator();

            string parsing = "parsing |";
            foreach (T s in input)
            {
                parsing += s + "|";
            }
            Print.Log(parsing);

            do
            {
                quantity = Parse(ref itr, quantity);
            }
            while (itr != null);

            return quantity;
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

        private T Parse(ref IEnumerator<T> itr, T firstInList = default)
        {
            System.BiEnumerable.LinkedList<object> input = new System.BiEnumerable.LinkedList<object>();
            if (!EqualityComparer<T>.Default.Equals(default, firstInList))
            {
                input.AddFirst(firstInList);
            }
            LinkedList<LinkedListNode<object>>[] operations = new LinkedList<LinkedListNode<object>>[Count];
            //Evaluator quantity = new Evaluator(list, new LinkedList<LinkedListNode<object>>[Count]);

            while (true)
            {
                if (itr == null || !itr.MoveNext())
                {
                    itr = null;
                }

                if (itr == null || Closing.Contains(itr.Current))
                //if (classified == Classification.Opening)
                {
                    //Evaluator e = quantity;
                    Print.Log("close", input.Count);
                    LinkedListNode<object> a = input.First;
                    while (a != null)
                    {
                        Print.Log(a.Value, a.Value?.GetType());
                        a = a.Next;
                    }
                    Print.Log("\n");

                    //Other.LinkedList<object> input1 = input;
                    return Close(input, operations);
                    //return e.Input.First.Value;
                }
                //else if (classified == Classification.Closing)
                else if (Opening.Contains(itr.Current))
                {
                    Print.Log("open");

                    input.AddLast(Parse(ref itr));
                    //LinkedList<object> e = Parse(ref itr);
                    //quantity.Input.AddLast(Delist(e));
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
                        if (operations[index] == null)
                        {
                            operations[index] = new LinkedList<LinkedListNode<object>>();
                        }
                        LinkedList<LinkedListNode<object>> list = operations[index];

                        if (operation.Order == ProcessingOrder.RightToLeft)
                        {
                            list.AddFirst(node);
                        }
                        else
                        {
                            list.AddLast(node);
                        }

                        input.AddLast(node);
                    }
                    else
                    {
                        Print.Log("found operand", next);

                        foreach (T o in ParseOperand(next))
                        {
                            Print.Log("\t" + o);
                            input.AddLast(o);
                        }
                    }
                }
            }
        }

        private T Close(IOrdered<object> input, LinkedList<LinkedListNode<object>>[] operations)
        {
            for (int j = 0; j < operations.Length; j++)
            {
                LinkedList<LinkedListNode<object>> stack = operations[j];

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

            if (input.Count == 0)
            {
                throw new Exception();
            }

            //IOrdered<object> itr = new LinkedListBiEnumerator<object>(input);
            //itr.MoveNext();
            //Other.LinkedList<object> list = input;
            return Juxtapose(input);
        }

        /*private class Evaluator
        {
            public LinkedList<object> Input;
            public LinkedList<LinkedListNode<object>>[] Operations;

            public Evaluator(LinkedList<object> input, LinkedList<LinkedListNode<object>>[] operations)
            {
                Input = input;
                Operations = operations;
            }
        }*/
    }
}
#endif