﻿using System;
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
        private KeyValuePair<TInput, Operator<TOutput>>[][] data;

        public Reader(params KeyValuePair<TInput, Operator<TOutput>>[][] data) : this(new Dictionary<TInput, Operator<TOutput>>()) { }

        protected Reader(IDictionary<TInput, Operator<TOutput>> operations, params KeyValuePair<TInput, Operator<TOutput>>[][] data)
        {
            Operations = operations;
            dict = new Dictionary<TInput, int>();
            this.data = data;

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

        protected virtual TOutput Juxtapose(IEditEnumerator<object> expression) => throw new Exception();

        public TOutput Parse(IEditEnumerable<TInput> input)
        {
            //return Parse1(input);

            System.BiEnumerable.LinkedList<object> list = new System.BiEnumerable.LinkedList<object>();
            foreach(TInput t in input)
            {
                list.AddLast(t);
            }
            return Parse(list.GetEnumerator());
            //return data[0][0].Value.Order == ProcessingOrder.LeftToRight ? Parse(input.GetEnumerator()) : Parse(input.GetEnumerator());
        }

        public TOutput Parse(IEditEnumerator<object> input)
        {
            string parsing = "parsing section |";

            IEditEnumerator<object> itr;
            IEditEnumerator<object> start = input.Copy();
            IEditEnumerator<object> end = input.Copy();
            while (end.MoveNext())
            {
                if (end.Current is TInput)
                {
                    TInput current = (TInput)end.Current;

                    if (Closing.Contains((TInput)end.Current))
                    {
                        break;
                    }
                    else if (Opening.Contains(current))
                    {
                        end.Add(1, Parse(end.Copy()));
                        end.Move(1);
                        end.Remove(-1);
                    }
                    else if (!Operations.ContainsKey(current))
                    {
                        end.Move(-1);
                        end.Remove(1);

                        string last = "";
                        foreach (TOutput o in ParseOperand(current))
                        {
                            parsing += last;
                            end.Add(1, o);
                            end.Move(1);
                            last = o + "|";
                        }
                    }
                }

                parsing += end.Current + "|";
            }
            Print.Log(parsing, end.Current);

            int direction;

            for (int i = 0; i < data.Length; i++)
            {
                /*if ((data[i][0].Value.Order == ProcessingOrder.RightToLeft && direction == -1) || (data[i][0].Value.Order == ProcessingOrder.LeftToRight && direction == 1))
                {
                    direction *= -1;
                    while (itr.Move(direction) && !Closing.Contains(itr.Current)) { }
                }*/

                if (data[i][0].Value.Order == ProcessingOrder.LeftToRight)
                {
                    itr = start.Copy();
                    direction = 1;
                }
                else
                {
                    itr = end.Copy();
                    direction = -1;
                }

                while (itr.Move(direction))
                {
                    if (itr.Current is TInput)
                    {
                        TInput current = (TInput)itr.Current;

                        if ((direction == 1 && Closing.Contains(current)) || (direction == -1 && Opening.Contains(current)))
                        {
                            if (i + 1 == data.Length)
                            {
                                itr.Remove();
                            }
                            break;
                        }
                        /*else if ((direction == -1 && Closing.Contains(current)) || (direction == 1 && Opening.Contains(current)))
                        {
                            itr.Add(direction, Parse(itr));
                            itr.Move(direction);
                            itr.Remove(-direction);
                        }*/
                        else
                        {
                            Operator<TOutput> operation;

                            if (Operations.TryGetValue(current, out operation))
                            {
                                //Print.Log("found operator", itr.Current);

                                foreach (KeyValuePair<TInput, Operator<TOutput>> kvp in data[i])
                                {
                                    if (kvp.Key.Equals(current))
                                    {
                                        Print.Log("doing operation", current);

                                        IEditEnumerator<object>[] operandItrs = new IEditEnumerator<object>[kvp.Value.Targets.Length];
                                        for (int j = 0; j < kvp.Value.Targets.Length; j++)
                                        {
                                            operandItrs[j] = itr.Copy();
                                            kvp.Value.Targets[j](operandItrs[j]);
                                        }

                                        TOutput[] operands = new TOutput[operandItrs.Length];
                                        for (int j = 0; j < operandItrs.Length; j++)
                                        {
                                            Print.Log("\t" + operandItrs[j].Current);
                                            operands[j] = (TOutput)operandItrs[j].Current;
                                            operandItrs[j].Remove();
                                        }

                                        itr.Add(1, kvp.Value.Operate(operands));
                                        itr.MoveNext();
                                        itr.Remove(-1);

                                        break;
                                    }
                                }
                            }
                            else
                            {

                            }
                        }
                    }
                }
            }
            Print.Log("done");
            IEditEnumerator<object> printer = start.Copy();
            while (printer.MoveNext())
            {
                Print.Log("\t" + printer.Current);
            }

            if (!start.MoveNext() || !(start.Current is TOutput))
            {
                throw new Exception();
            }
            else
            {
                start.MovePrev();
                return Juxtapose(start);
            }
        }

        public TOutput Parse1(IEditEnumerable<TInput> input)
        {
            string parsing = "parsing |";
            foreach (TInput s in input)
            {
                parsing += s + "|";
            }
            Print.Log(parsing);

            Stack<Evaluator> quantities = new Stack<Evaluator>();

            IEditEnumerator<TInput> itr = input.GetEnumerator();
            quantities.Push(new Evaluator(new System.BiEnumerable.LinkedList<object>(), new LinkedList<LinkedListNode<object>>[Count], new LinkedList<Operator<TOutput>>[Count]));

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
                            quantities.Push(new Evaluator(front, new LinkedList<LinkedListNode<object>>[Count], new LinkedList<Operator<TOutput>>[Count]));
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
                    quantities.Push(new Evaluator(new System.BiEnumerable.LinkedList<object>(), new LinkedList<LinkedListNode<object>>[Count], new LinkedList<Operator<TOutput>>[Count]));
                }
                else
                {
                    Operator<TOutput> operation;
                    if (Operations.TryGetValue(itr.Current, out operation))
                    {
                        Print.Log("found operator", itr.Current);

                        int index = IndexOf(itr.Current);

                        // Put the operator in the linked list as a node
                        LinkedListNode<object> node = new LinkedListNode<object>(itr.Current);

                        // Get the list of all of this type of operator (e.g. all instances of "+")
                        if (quantity.Operations[index] == null)
                        {
                            quantity.Operations[index] = new LinkedList<LinkedListNode<object>>();
                            quantity.Operators[index] = new LinkedList<Operator<TOutput>>();
                        }

                        if (operation.Order == ProcessingOrder.RightToLeft)
                        {
                            quantity.Operations[index].AddFirst(node);
                            quantity.Operators[index].AddFirst(operation);
                        }
                        else
                        {
                            quantity.Operations[index].AddLast(node);
                            quantity.Operators[index].AddLast(operation);
                        }

                        quantity.Input.AddLast(node);
                    }
                    else
                    {
                        Print.Log("found operand", itr.Current);

                        foreach (TOutput o in ParseOperand(itr.Current))
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

        private TOutput Close(Evaluator quantity)
        {
            for (int j = 0; j < quantity.Operations.Length; j++)
            {
                LinkedList<LinkedListNode<object>> stack = quantity.Operations[j];
                
                while (stack?.Count > 0)
                {
                    LinkedListNode<object> node = stack.Dequeue().Value;
                    Operator<TOutput> op = quantity.Operators[j].Dequeue().Value;

                    // Operator was removed by someone else
                    if (node.List == null)
                    {
                        continue;
                    }
                    //Operator<TOutput> op = (Operator<TOutput>)node.Value;
                    
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
                        operandNodes[k].Remove();
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
            return Juxtapose(quantity.Input.GetEnumerator());
        }

        private class Evaluator
        {
            public System.BiEnumerable.LinkedList<object> Input;
            public LinkedList<LinkedListNode<object>>[] Operations;
            public LinkedList<Operator<TOutput>>[] Operators;

            public Evaluator(System.BiEnumerable.LinkedList<object> input, LinkedList<LinkedListNode<object>>[] operations, LinkedList<Operator<TOutput>>[] operators)
            {
                Input = input;
                Operations = operations;
                Operators = operators;
            }
        }
    }
}
#endif