using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Extensions;

#if DEBUG
namespace Parse
{
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

        protected virtual TOutput Juxtapose(IEditEnumerable<object> expression) => throw new Exception();

        /*public TOutput Parse(IEnumerable<TInput> input)
        {
            TOutput quantity = default;

            IEnumerator<TInput> itr = input.GetEnumerator();
            


            return Iterative(input);

            do
            {
                quantity = Parse(ref itr, quantity);
            }
            while (itr != null);

            return quantity;
        }*/

        public TOutput Parse(IEnumerable<TInput> input)
        {
            string parsing = "parsing |";
            foreach (TInput s in input)
            {
                parsing += s + "|";
            }
            Print.Log(parsing);

            Stack<Evaluator> quantities = new Stack<Evaluator>();

            IEnumerator<TInput> itr = input.GetEnumerator();
            quantities.Push(new Evaluator(new System.BiEnumerable.LinkedList<object>(), new LinkedList<LinkedListNode<object>>[Count]));

            while (true)
            {
                Evaluator quantity = quantities.Peek();

                /*if (itr == null || !itr.MoveNext())
                {
                    itr = null;
                }*/

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

                    TOutput answer = Close(e);

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

                    //Other.LinkedList<object> input1 = input;
                    //return Close(quantity);
                    //return e.Input.First.Value;
                }
                else if (Opening.Contains(itr.Current))
                {
                    Print.Log("open");

                    quantities.Push(new Evaluator(new System.BiEnumerable.LinkedList<object>(), new LinkedList<LinkedListNode<object>>[Count]));

                    //quantity.Input.AddLast(Parse(ref itr));
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

                        foreach (TOutput o in ParseOperand(next))
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

        private TOutput Parse(ref IEnumerator<TInput> itr, TOutput firstInList = default)
        {
            System.BiEnumerable.LinkedList<object> input = new System.BiEnumerable.LinkedList<object>();
            if (!EqualityComparer<TOutput>.Default.Equals(default, firstInList))
            {
                input.AddFirst(firstInList);
            }
            //LinkedList<LinkedListNode<object>>[] operations = new LinkedList<LinkedListNode<object>>[Count];
            Evaluator quantity = new Evaluator(input, new LinkedList<LinkedListNode<object>>[Count]);

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
                    Print.Log("close", quantity.Input.Count);
                    LinkedListNode<object> a = quantity.Input.First;
                    while (a != null)
                    {
                        Print.Log(a.Value, a.Value?.GetType());
                        a = a.Next;
                    }
                    Print.Log("\n");

                    //Other.LinkedList<object> input1 = input;
                    return Close(quantity);
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

                        foreach (TOutput o in ParseOperand(next))
                        {
                            Print.Log("\t" + o);
                            quantity.Input.AddLast(o);
                        }
                    }
                }
            }
        }

        private TOutput Close(Evaluator quantity)
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
                    Operator<TOutput> op = (Operator<TOutput>)node.Value;
                    
                    IEditEnumerator<object>[] operandNodes = new IEditEnumerator<object>[op.Targets.Length];
                    
                    for (int k = 0; k < op.Targets.Length; k++)
                    {
                        operandNodes[k] = new System.BiEnumerable.LinkedList<object>.Enumerator(node);
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