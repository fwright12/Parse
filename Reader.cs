using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Extensions;

#if DEBUG
namespace Parse
{
    public enum Member { Opening = -1, Closing = 1, Operand, Operator };

    public abstract class Reader<TInput, TOutput>
    {
        public HashSet<TInput> Opening;
        public HashSet<TInput> Closing;
        public HashSet<TInput> Ignore;

        public readonly IDictionary<TInput, Tuple<Operator<TOutput>, int>> Operations;

        protected Reader(IDictionary<TInput, Tuple<Operator<TOutput>, int>> operations)
        {
            Operations = operations;

            Opening = new HashSet<TInput>();
            Closing = new HashSet<TInput>();
            Ignore = new HashSet<TInput>();

            foreach (KeyValuePair<TInput, Tuple<Operator<TOutput>, int>> kvp in Operations)
            {
                if (Opening.Contains(kvp.Key) || Closing.Contains(kvp.Key))
                {
                    throw new Exception("The character " + kvp + " cannot appear in a command - this character is used to separate quantities");
                }
                /*else if (kvp.Value.Item1.Order != Operations[i].First().Value.Item1.Order)
                {
                    throw new Exception("All operators in a tier must process in the same direction");
                }*/
            }
        }

        protected TOutput ParseOperand(object operand) => operand is TOutput ? (TOutput)operand : ParseOperand((TInput)operand);
        protected abstract TOutput ParseOperand(TInput operand);
        public TOutput ParseOperandUnsafe(IEditEnumerator<object> itr)
        {
            Member member = Classify(itr.Current);
            if (member == Member.Opening || member == Member.Closing)
            {
                itr.Add(0, Parse(itr, (ProcessingOrder)(-(int)member)));
            }

            return ParseOperand(itr.Current);
        }

        protected virtual TOutput Juxtapose(IEnumerable<TOutput> expression) => throw new Exception();
        public IEnumerable<TOutput> CollectOperands(IEditEnumerator<object> itr, ProcessingOrder direction = ProcessingOrder.LeftToRight)
        {
            while (itr.Move((int)direction))
            {
                Member member = Classify(itr.Current);

                if (member == Member.Operand || (int)direction == -(int)member)
                {
                    
                }
                else
                {
                    break;
                }
                /*if (itr.Current is TInput && Closing.Contains((TInput)itr.Current))
                {
                    itr.Remove(0);
                    break;
                }*/

                //yield return itr.Current is TOutput ? (TOutput)itr.Current : ParseOperand((TInput)itr.Current);
                //yield return (TOutput)itr.Current;
                //yield return ParseOperand(itr.Current);
                yield return ParseOperandUnsafe(itr);

                itr.Move(-(int)direction);
                itr.Remove((int)direction);
            }
        }

        public Member Classify(object input) => input is TOutput ? Member.Operand : Classify((TInput)input);

        public Member Classify(TInput input)
        {
            if (Opening.Contains(input))
            {
                return Member.Opening;
            }
            else if (Closing.Contains(input))
            {
                return Member.Closing;
            }
            else if (Operations.ContainsKey(input))
            {
                return Member.Operator;
            }
            else
            {
                return Member.Operand;
            }
        }

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

        private TOutput Parse(IEditEnumerator<object> input, ProcessingOrder direction = ProcessingOrder.LeftToRight)
        {
            string parsing = "";
            SortedDictionary<int, ProcessingOrder> order = new SortedDictionary<int, ProcessingOrder>();

            IEditEnumerator<object> itr;
            IEditEnumerator<object> start = input.Copy();
            IEditEnumerator<object> end = input.Copy();

            int count = 0;
            while (end.Move((int)direction))
            {
                if (direction == ProcessingOrder.LeftToRight)
                {
                    parsing += end.Current + "|";
                }
                else
                {
                    parsing = end.Current + "|" + parsing;
                }

                if (!(end.Current is TInput))
                {
                    continue;
                }

                TInput current = (TInput)end.Current;
                Member member = Classify(current);

                if ((int)direction == (int)member)
                {
                    if (count == 0)
                    {
                        break;
                    }
                    else
                    {
                        count--;
                    }
                }
                else if ((int)direction == -(int)member)
                {
                    count++;
                    //end.Add(0, Parse(end.Copy()));
                }
                else
                {
                    Tuple<Operator<TOutput>, int> temp;
                    if (Operations.TryGetValue(current, out temp))
                    {
                        order[temp.Item2] = temp.Item1.Order;
                    }
                    else
                    {
                        //end.Add(0, ParseOperand(current));
                    }
                }
            }

            if (direction == ProcessingOrder.RightToLeft)
            {
                Misc.Swap(ref start, ref end);
            }

            Print.Log("parsing section |" + parsing, end.Current);
            Print.Log("start is " + start.Current + " and end is " + end.Current);

            foreach (KeyValuePair<int, ProcessingOrder> kvp in order)
            {
                if (kvp.Value == ProcessingOrder.LeftToRight)
                {
                    itr = start.Copy();
                }
                else
                {
                    itr = end.Copy();
                }

                while (itr.Move((int)kvp.Value))
                {
                    if (!(itr.Current is TInput))
                    {
                        continue;
                    }

                    TInput current = (TInput)itr.Current;
                    Tuple<Operator<TOutput>, int> tuple;

                    //if ((kvp.Value == ProcessingOrder.LeftToRight && Closing.Contains(current)) ||
                      //  (kvp.Value == ProcessingOrder.RightToLeft && Opening.Contains(current)))
                    if (itr.Equals(start) || itr.Equals(end))
                    {
                        break;
                    }
                    else if (Operations.TryGetValue(current, out tuple) && kvp.Key == tuple.Item2)
                    {
                        Print.Log("doing operation", current);

                        Operator<TOutput> operation = tuple.Item1;
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
                            operands[j] = ParseOperandUnsafe(operandItrs[j]); //ParseOperand(operandItrs[j].Current);
                            //operands[j] = operandItrs[j].Current is TOutput ? (TOutput)operandItrs[j].Current : ParseOperand((TInput)operandItrs[j].Current);
                            operandItrs[j].Remove(0);
                        }

                        itr.Add(0, operation.Operate(operands));
                    }
                }
            }

            if (direction == ProcessingOrder.RightToLeft)
            {
                Misc.Swap(ref start, ref end);
            }

            Print.Log("done");
            IEditEnumerator<object> printer = start.Copy();
            while (printer.MoveNext())
            {
                Print.Log("\t" + printer.Current);
            }

            TOutput result = Juxtapose(CollectOperands(start, direction));
            Print.Log("start is " + start.Current);
            start.Remove(0);
            return result;
        }
    }
}
#endif