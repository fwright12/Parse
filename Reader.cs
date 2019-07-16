using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Extensions;
//using Crunch.Machine;

#if DEBUG
namespace Parse
{
    //using Operator = Crunch.Machine.Operator;

    public abstract class Reader<TInput, TOutput>
    {
        public int Count => dict.Count;

        //protected Trie<Operator> Operations;
        readonly protected IDictionary<TInput, Operator<TOutput>> Operations;
        //protected IDictionary<TOperation, Operator> Operations;

        public HashSet<TInput> Opening;
        public HashSet<TInput> Closing;
        public HashSet<TInput> Ignore;

        private Dictionary<TInput, int> dict;
        Operator<TOutput> juxtapose;
        int multiplication => IndexOf((dynamic)"*");

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

            Operations.TryGetValue((dynamic)"*", out juxtapose);
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

        public object Parse(IEnumerable<TInput> input)
        {
            object quantity = null;

            IEnumerator<TInput> itr = Next(input).GetEnumerator();

            string parsing = "parsing |";
            foreach (TInput s in Next(input))
            {
                parsing += s + "|";
            }
            Print.Log(parsing);

            do
            {
                if (quantity != null)
                {
                    //object o = Delist(quantity);
                    //quantity = new LinkedList<object>();
                    //quantity.AddFirst(o);
                }

                quantity = Parse(ref itr, quantity);
            }
            while (itr != null);

            return quantity;
        }

        //private object Delist(LinkedList<object> list) => list.First.Value; // list.Count == 1 ? (object)list.First.Value : list;

        private IEnumerable<TInput> Next(IEnumerable<TInput> input)
        {
            foreach(TInput t in input)
            {
                if (!Ignore.Contains(t))
                {
                    yield return t;
                }
            }
        }

        /*private void Next(ref IEnumerator<T> itr, out T next)
        {
            next = default;

            do
            {
                if (itr == null || !itr.MoveNext())
                {
                    itr = null;
                    return;// Classification.Other;
                }

                next = itr.Current;
            }
            while (itr != null && Skip.Contains(next));

            return;// Classification.Other;
        }*/

        private object Parse(ref IEnumerator<TInput> itr, object firstInList = null)
        {
            LinkedList<object> list = new LinkedList<object>();
            if (firstInList != null)
            {
                list.AddFirst(firstInList);
            }
            Evaluator quantity = new Evaluator(list, new LinkedList<LinkedListNode<object>>[Count]);

            while (true)
            {
                if (itr == null || !itr.MoveNext())
                {
                    itr = null;
                }

                if (itr == null || Closing.Contains(itr.Current))
                //if (classified == Classification.Opening)
                {
                    Evaluator e = quantity;
                    Print.Log("close", e.Input.Count);
                    LinkedListNode<object> a = e.Input.First;
                    while (a != null)
                    {
                        Print.Log(a.Value, a.Value?.GetType());
                        a = a.Next;
                    }
                    Print.Log("\n");

                    return Close(e);
                    //return e.Input.First.Value;
                }
                //else if (classified == Classification.Closing)
                else if (Opening.Contains(itr.Current))
                {
                    Print.Log("open");

                    quantity.Input.AddLast(Parse(ref itr));
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
                        if (quantity.Operations[index] == null)
                        {
                            quantity.Operations[index] = new LinkedList<LinkedListNode<object>>();
                        }
                        var input = quantity.Operations[index];

                        if (operation.Order == ProcessingOrder.RightToLeft)
                        {
                            input.AddFirst(node);
                        }
                        else
                        {
                            input.AddLast(node);
                        }

                        quantity.Input.AddLast(node);
                    }
                    else
                    {
                        Print.Log("found operand", next);

                        foreach (TOutput o in ParseOperand(next))
                        {
                            Print.Log("\t" + o);
                            quantity.Input.AddLast(o);
                        }
                    }
                }
            }
        }

        /*private LinkedList<object> Parse(ref List<string> input, ref int i)
        {
            string parsing = "parsing |";
            foreach (string s in input)
            {
                parsing += s + "|";
            }
            Print.Log(parsing);

            Evaluator<object> quantity = new Evaluator<object>(new LinkedList<object>(), new LinkedList<LinkedListNode<object>>[Count]);

            Operator juxtapose;
            Operations.TryGetValue("*", out juxtapose);
            int multiplication = IndexOf("*");

            HashSet<char> terminaters = new HashSet<char>() { '(', ')' };
            HashSet<char> ignored = new HashSet<char>() { ' ' };

            string OPEN = "-1";
            string CLOSE = "-2";

            while (i <= input.Count)
            {
                string c = "";

                do
                {
                    if (i >= input.Count || input[i][0].IsClosing())
                    {
                        c = CLOSE;
                    }
                    else if (input[i][0].IsOpening())
                    {
                        c = OPEN;
                    }
                    else
                    {
                        c = input[i];
                    }

                    i++;
                }
                while (c[0] == ' ');

                Operator temp;
                if (Operations.TryGetValue(c, out temp) == TrieContains.Full)
                {
                    // Operator

                    int index = IndexOf(c);
                    LinkedListNode<object> node = AddOperator(quantity, temp, index, c);
                    quantity.Input.AddLast(node);
                }
                else
                {
                    // Operand

                    foreach (object o in ParseOperandString(c))
                    {
                        Print.Log("\t" + o);
                        quantity.Input.AddLast(o);
                    }
                }

                if (c == OPEN)
                {
                    Print.Log("open");

                    LinkedList<object> e = Parse(ref input, ref i);
                    quantity.Input.AddLast(Delist(e));
                }
                else if (c == CLOSE)
                {
                    Evaluator<object> e = quantity;
                    Print.Log("close", e.Input.Count);
                    LinkedListNode<object> a = e.Input.First;
                    while (a != null)
                    {
                        Print.Log(a.Value, a.Value?.GetType());
                        a = a.Next;
                    }
                    Print.Log("\n");

                    Close(e, multiplication, juxtapose);
                    return e.Input;
                }
            }

            throw new Exception("Error parsing math");
        }*/

        private object Close(Evaluator e)
        {
            for (int j = 0; j < e.Operations.Length; j++)
            {
                LinkedList<LinkedListNode<object>> stack = e.Operations[j];
                
                while (stack?.Count > 0)
                {
                    LinkedListNode<object> node = stack.Dequeue().Value;
                    if (node.List == null)
                    {
                        continue;
                    }
                    Operator<TOutput> op = (Operator<TOutput>)node.Value;
                    
                    LinkedListNode<object>[] operandNodes = new LinkedListNode<object>[op.Targets.Length];

                    for (int k = 0; k < op.Targets.Length; k++)
                    {
                        operandNodes[k] = op.Targets[k](node);
                    }

                    TOutput[] operands = new TOutput[operandNodes.Length];
                    for (int k = 0; k < operands.Length; k++)
                    {
                        operands[k] = (TOutput)operandNodes[k]?.Value;
                        if (operandNodes[k] != null)
                        {
                            e.Input.Remove(operandNodes[k]);
                        }
                    }

                    Print.Log("operating", op.GetType(), operands.Length);
                    foreach(object o in operands)
                    {
                        Print.Log(o, o.GetType());
                    }
                    Print.Log("done");

                    node.Value = op.Operate(operands);
                }

                if (j == multiplication)
                {
                    Juxtapose(e.Input, juxtapose);
                }
            }

            return e.Input.First.Value;
        }

        private void Juxtapose(LinkedList<object> expression, Operator<TOutput> juxtapse)
        {
            LinkedListNode<object> node = expression.First;

            while (node.Next != null)
            {
                if (node.Value is Operator<TOutput> || node.Next.Value is Operator<TOutput>)
                {
                    node = node.Next;
                }
                else
                {
                    node.Value = juxtapse.Operate((TOutput)node.Value, (TOutput)node.Next.Value);
                    expression.Remove(node.Next);
                }
            }
        }

        /*private string Search(string input)
        {
            //Keep track of the end position of the longest possible operation we find
            int operation = 0;
            for (int i = 0; i < input.Length; i++)
            {
                Operator temp;
                TrieContains search = Operations.TryGetValue(input.Substring(0, i + 1), out temp);
                //At this point there is no operation that starts like this
                if (search == TrieContains.No)
                {
                    break;
                }
                //We found an operation, but it might not be the longest one
                if (search == TrieContains.Full)
                {
                    operation = i + 1;
                }
            }

            return input.Substring(0, Math.Max(1, operation));
        }*/

        private class Evaluator
        {
            public LinkedList<object> Input;
            public LinkedList<LinkedListNode<object>>[] Operations;

            public Evaluator(LinkedList<object> input, LinkedList<LinkedListNode<object>>[] operations)
            {
                Input = input;
                Operations = operations;
            }
        }
    }
}
#endif