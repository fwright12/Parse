using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Extensions;
using System.Collections;
//using Crunch.Machine;

#if DEBUG
namespace Parse
{
    public interface IOrdered<T> : IEnumerator<T>
    {
        bool Move(int n);
        bool MovePrev();

        void Add(int n, T t);
        bool Remove(int n);
    }

    public class LinkedListBiEnumerator<T> : IOrdered<T>
    {
        public T Current => Node == null ? default : Node.Value;
        object IEnumerator.Current => throw new NotImplementedException();

        private LinkedList<T> List;
        private LinkedListNode<T> Node;
        private bool begin;

        public LinkedListBiEnumerator(LinkedList<T> list)
        {
            List = list;
            begin = true;
        }

        public LinkedListBiEnumerator(LinkedListNode<T> node) : this(node.List)
        {
            Node = node;
        }

        private LinkedListBiEnumerator(LinkedListBiEnumerator<T> itr)
        {
            List = itr.List;
            Node = itr.Node;
            begin = itr.begin;
        }

        public void Add(int n, T t)
        {
            if (n == 0)
            {
                return;
            }
            
            LinkedListBiEnumerator<T> itr = Node == null ? null : new LinkedListBiEnumerator<T>(Node);
            if (itr == null || !itr.Move(n - Math.Sign(n)))
            {
                if (begin)
                {
                    List.AddFirst(t);
                }
                else
                {
                    List.AddLast(t);
                }
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
            LinkedListBiEnumerator<T> itr = new LinkedListBiEnumerator<T>(this);
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
        }
    }

    public abstract class Reader<TInput, TOutput>
    {
        public int Count => dict.Count;

        readonly protected IDictionary<TInput, Operator<TOutput>> Operations;

        public HashSet<TInput> Opening;
        public HashSet<TInput> Closing;
        public HashSet<TInput> Ignore;

        private Dictionary<TInput, int> dict;

        public Reader(params KeyValuePair<TInput, Operator<TOutput>>[][] data) : this(new Dictionary<TInput, Operator<TOutput>>()) { }

        protected Reader(IDictionary<TInput, Operator<TOutput>> operations, params KeyValuePair<TInput, Operator<TOutput>>[][] data)
        {
            Operations = operations;
            dict = new Dictionary<TInput, int>();
            
            Opening = new HashSet<TInput>();
            Closing = new HashSet<TInput>();
            Ignore = new HashSet<TInput>();

            for (int i = 0; i < data.Length; i++)
            {
                foreach (KeyValuePair<TInput, Operator<TOutput>> kvp in data[i])
                {
                    if (Opening.Contains(kvp.Key) || Closing.Contains(kvp.Key))
                    {
                        throw new Exception("The character " + kvp + " cannot appear in a command - this character is used to separate quantities");
                    }

                    Insert(i, kvp.Key, kvp.Value);
                }
            }
        }

        public void Add(TInput symbol, Operator<TOutput> operation) => Insert(dict.Count, symbol, operation);

        public void Insert(int index, TInput symbol, Operator<TOutput> operation)
        {
            KeyValuePair<TInput, Operator<TOutput>> pair = new KeyValuePair<TInput, Operator<TOutput>>(symbol, operation);
            dict.Add(pair.Key, index);
            Operations.Add(pair.Key, pair.Value);
        }

        private int IndexOf(TInput key) => dict.ContainsKey(key) ? dict[key] : -1;

        protected abstract IEnumerable<TOutput> ParseOperand(TInput operand);

        protected virtual TOutput Juxtapose(IOrdered<object> expression) => throw new Exception();

        public TOutput Parse(IEnumerable<TInput> input)
        {
            TOutput quantity = default;

            IEnumerator<TInput> itr = input.GetEnumerator();

            string parsing = "parsing |";
            foreach (TInput s in input)
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

        private TOutput Parse(ref IEnumerator<TInput> itr, TOutput firstInList = default)
        {
            LinkedList<object> input = new LinkedList<object>();
            if (!EqualityComparer<TOutput>.Default.Equals(default, firstInList))
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
                    TInput next = itr.Current;

                    Operator<TOutput> operation;
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

                        foreach (TOutput o in ParseOperand(next))
                        {
                            Print.Log("\t" + o);
                            input.AddLast(o);
                        }
                    }
                }
            }
        }

        private TOutput Close(LinkedList<object> input, LinkedList<LinkedListNode<object>>[] operations)
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
                    Operator<TOutput> op = (Operator<TOutput>)node.Value;
                    
                    IOrdered<object>[] operandNodes = new IOrdered<object>[op.Targets.Length];
                    
                    for (int k = 0; k < op.Targets.Length; k++)
                    {
                        operandNodes[k] = new LinkedListBiEnumerator<object>(node);
                        op.Targets[k](operandNodes[k]);
                    }

                    TOutput[] operands = new TOutput[operandNodes.Length];
                    for (int k = 0; k < operands.Length; k++)
                    {
                        operands[k] = (TOutput)operandNodes[k].Current;
                        operandNodes[k].Remove(0);
                    }

                    Print.Log("operating", op.GetType(), operands.Length);
                    foreach(object o in operands)
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

            IOrdered<object> itr = new LinkedListBiEnumerator<object>(input);
            //itr.MoveNext();
            return Juxtapose(itr);
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