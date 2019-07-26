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
        public HashSet<TInput> Opening;
        public HashSet<TInput> Closing;
        public HashSet<TInput> Ignore;

        readonly public IDictionary<TInput, Operator<TOutput>> Operations;
        private IDictionary<TInput, Operator<TOutput>>[] OrderOfOperations;

        public Reader(params IDictionary<TInput, Operator<TOutput>>[] data) : this(new Dictionary<TInput, Operator<TOutput>>(), data) { }
        public Reader(IDictionary<TInput, Operator<TOutput>> operations)
        {
            Operations = operations;
        }

        protected Reader(IDictionary<TInput, Operator<TOutput>> operations, params IDictionary<TInput, Operator<TOutput>>[] orderOfOperations)
        {
            Operations = operations;
            OrderOfOperations = orderOfOperations;

            Opening = new HashSet<TInput>();
            Closing = new HashSet<TInput>();
            Ignore = new HashSet<TInput>();

            /*for (int i = 1; i < OrderOfOperations.Length; i++)
            {
                foreach(KeyValuePair<TInput, Operator<TOutput>> kvp in OrderOfOperations[i])
                {
                    OrderOfOperations[0].Add(kvp);
                }
            }

            Operations = OrderOfOperations[0];*/
            
            for (int i = 0; i < OrderOfOperations.Length; i++)
            {
                foreach (KeyValuePair<TInput, Operator<TOutput>> kvp in OrderOfOperations[i])
                {
                    if (Opening.Contains(kvp.Key) || Closing.Contains(kvp.Key))
                    {
                        throw new Exception("The character " + kvp + " cannot appear in a command - this character is used to separate quantities");
                    }
                    else if (kvp.Value.Order != OrderOfOperations[i].First().Value.Order)
                    {
                        throw new Exception("All operators in a tier must process in the same direction");
                    }

                    Operations.Add(kvp.Key, kvp.Value);
                }
            }
        }

        protected TOutput ParseOperand(object operand) => operand is TOutput ? (TOutput)operand : ParseOperand((TInput)operand);
        protected abstract TOutput ParseOperand(TInput operand);

        protected virtual TOutput Juxtapose(IEnumerable<TOutput> expression) => throw new Exception();

        public TOutput Parse(IEnumerable<object> input)
        {
            System.BiEnumerable.LinkedList<object> list = new System.BiEnumerable.LinkedList<object>();
            foreach(object t in input)
            {
                //Print.Log("\t" + t);
                list.AddLast(t);
            }
            return Parse(list.GetEnumerator());
        }

        private int Index(TInput input)
        {
            for (int i = 0; i < OrderOfOperations.Length; i++)
            {
                if (OrderOfOperations[i].ContainsKey(input))
                {
                    return i;
                }
            }

            return -1;
        }

        private TOutput Parse(IEditEnumerator<object> input)
        {
            string parsing = "parsing section |";
            SortedSet<int> order = new SortedSet<int>();

            IEditEnumerator<object> itr = null;
            IEditEnumerator<object> start = input.Copy();
            IEditEnumerator<object> end = input.Copy();
            while (end.MoveNext())
            {
                parsing += end.Current + "|";
                if (!(end.Current is TInput))
                {
                    continue;
                }

                TInput current = (TInput)end.Current;

                if (Closing.Contains((TInput)end.Current))
                {
                    break;
                }
                else if (Opening.Contains(current))
                {
                    end.Add(0, Parse(end.Copy()));
                }
                else //if (Operations.ContainsKey(current))
                {
                    int index = Index(current);

                    if (index == -1)
                    {
                        end.Add(0, ParseOperand(current));
                    }
                    else
                    {
                        order.Add(index);
                    }
                }
                /*else if (!Operations.ContainsKey(current))
                {
                    //end.Add(0, ParseOperand(current));
                }*/
                /*else if (!Operations.ContainsKey(current))
                {
                    end.Move(1);
                    end.Remove(-1);

                    string last = "";
                    //foreach (TOutput o in ParseOperand(current))
                    //{
                    TOutput o = ParseOperand(current);
                        parsing += last;
                        end.Add(-1, o);
                        last = o + "|";
                    //}

                    end.MovePrev();
                }*/
            }
            Print.Log(parsing, end.Current);

            int direction;

            //for (int i = 0; i < OrderOfOperations.Length; i++)
            //for (int j = 0; j < order.Count; j++)
            foreach (int i in order)
            {
                //Print.Log(i, order[i]);
                /*if (!order[i])
                {
                    continue;
                }*/

                if (OrderOfOperations[i].First().Value.Order == ProcessingOrder.LeftToRight)
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
                    if (!(itr.Current is TInput))
                    {
                        continue;
                    }

                    TInput current = (TInput)itr.Current;
                    Operator<TOutput> operation;

                    if ((direction == 1 && Closing.Contains(current)) || (direction == -1 && Opening.Contains(current)))
                    {
                        if (i + 1 == OrderOfOperations.Length)
                        //if (i == order.Last())
                        {
                            //itr.Remove(0);
                        }
                        break;
                    }
                    /*else if ((direction == -1 && Closing.Contains(current)) || (direction == 1 && Opening.Contains(current)))
                    {
                        itr.Add(direction, Parse(itr));
                        itr.Move(direction);
                        itr.Remove(-direction);
                    }*/
                    else if (OrderOfOperations[i].TryGetValue(current, out operation))
                    {
                        Print.Log("doing operation", current);

                        IEditEnumerator<object>[] operandItrs = new IEditEnumerator<object>[operation.Targets.Length];
                        for (int j = 0; j < operation.Targets.Length; j++)
                        {
                            operandItrs[j] = itr.Copy();
                            operation.Targets[j](operandItrs[j]);
                        }

                        TOutput[] operands = new TOutput[operandItrs.Length];
                        for (int j = 0; j < operandItrs.Length; j++)
                        {
                            Print.Log("\t" + operandItrs[j].Current);
                            operands[j] = operandItrs[j].Current is TOutput ? (TOutput)operandItrs[j].Current : ParseOperand((TInput)operandItrs[j].Current);
                            operandItrs[j].Remove(0);
                        }

                        itr.Add(0, operation.Operate(operands));
                    }
                }
            }

            //end.Copy().Remove(0);

            Print.Log("done");
            IEditEnumerator<object> printer = start.Copy();
            while (printer.MoveNext())
            {
                Print.Log("\t" + printer.Current);
            }

            return Juxtapose(test(start));

            if (test(start.Copy()).GetEnumerator().MoveNext())
            {
                //throw new Exception();
            }
            else
            {
                //return Juxtapose1(test(start));
            }
        }

        private IEnumerable<TOutput> test(IEditEnumerator<object> itr)
        {
            while (itr.MoveNext())
            {
                if (itr.Current is TInput && Closing.Contains((TInput)itr.Current))
                {
                    itr.Remove(0);
                    break;
                }

                yield return itr.Current is TOutput ? (TOutput)itr.Current : ParseOperand((TInput)itr.Current);

                itr.MovePrev();
                itr.Remove(1);
            }
        }
    }
}
#endif