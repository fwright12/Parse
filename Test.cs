using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Extensions;
using System.Collections;

using Parse;

#if false
namespace Test
{
    public abstract class Reader1<TInput, TOutput> : Reader1<TOutput>
    {
        public Reader1(IDictionary<TOutput, Tuple<FunctionWithVariableParamterCount<TOutput>, Action<IEditEnumerator<TOutput>[], int>>> operations) : base(operations) { }
    }

    public abstract class Reader1<T> //: Reader1<T, T>
    {
        public Reader1(IDictionary<T, Tuple<FunctionWithVariableParamterCount<T>, Action<IEditEnumerator<T>[], int>>> operations) { }
    }

    public abstract class InPlaceReader<T>
    {
        public HashSet<T> Opening;
        public HashSet<T> Closing;
        public HashSet<T> Ignore;

        public readonly IDictionary<T, Tuple<Operator<T>, int>> Operations;

        protected InPlaceReader(IDictionary<T, Tuple<Operator<T>, int>> operations)
        {
            Operations = operations;

            Opening = new HashSet<T>();
            Closing = new HashSet<T>();
            Ignore = new HashSet<T>();

            foreach (KeyValuePair<T, Tuple<Operator<T>, int>> kvp in Operations)
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

        public T ParseOperand(IEditEnumerator<T> itr)
        {
            Member member = Classify(itr.Current);
            if (member == Member.Opening || member == Member.Closing)
            {
                itr.Add(0, Parse(itr, (ProcessingOrder)(-(int)member)));
            }

            return itr.Current;
        }

        protected virtual T Juxtapose(IEnumerable<T> expression) => expression.First();

        public IEnumerable<T> CollectOperands(IEditEnumerator<T> itr, ProcessingOrder direction = ProcessingOrder.LeftToRight)
        {
            while (itr.Move((int)direction))
            {
                Member member = Classify(itr.Current);
                // Stop if we get an operator or a bracket indicating the end of expression (which bracket depends on the processing direction)
                if (member == Member.Operator || (int)direction == (int)member)
                {
                    break;
                }

                yield return ParseOperand(itr);

                itr.Move(-(int)direction);
                itr.Remove((int)direction);
            }
        }

        public Member Classify(T input)
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

        public T Parse(IEditEnumerable<T> input) => Parse(input.GetEnumerator());

        private T Parse(IEditEnumerator<T> start, ProcessingOrder direction = ProcessingOrder.LeftToRight)
        {
            SortedDictionary<int, ProcessingOrder> order = new SortedDictionary<int, ProcessingOrder>();
            IEditEnumerator<T> end = start.Copy();
            int count = 0;

#if DEBUG
            string parsing = "";
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
            }

            Print.Log("parsing section |" + parsing);
            Print.Log("start is " + start.Current + " and end is " + end.Current);

            end = start.Copy();
#endif

            // Initial pass over the input to figure out:
            //      Where the beginning and end are (match parentheses)
            //      What operators we should look for (so we can skip iterating empty tiers)
            // Also delete anything that's supposed to be ignored
            while (end.Move((int)direction))
            {
                if (!(end.Current is T))
                {
                    continue;
                }

                T current = (T)end.Current;
                Member member = Classify(current);

                // This is the "close" bracket for the direction we're moving
                if ((int)direction == (int)member)
                {
                    // This is the end of the expression we're working on
                    if (count == 0)
                    {
                        break;
                    }
                    else
                    {
                        count--;
                    }
                }
                // This is the "open" bracket for the direction we're moving
                else if ((int)direction == -(int)member)
                {
                    count++;
                }
                else if (Ignore.Contains(current))
                {
                    end.Move(-1);
                    end.Remove(1);
                }
                // Keep track of what operators we find so we can skip them later
                else if (member == Member.Operator)
                {
                    Tuple<Operator<T>, int> temp = Operations[current];
                    order[temp.Item2] = temp.Item1.Order;
                }
            }

            foreach (KeyValuePair<int, ProcessingOrder> kvp in order)
            {
                IEditEnumerator<T> itr = kvp.Value == ProcessingOrder.LeftToRight ^ direction == ProcessingOrder.LeftToRight ? end.Copy() : start.Copy();

                while (itr.Move((int)kvp.Value) && !itr.Equals(start) && !itr.Equals(end))
                {
                    if (!(itr.Current is T))
                    {
                        continue;
                    }

                    Tuple<Operator<T>, int> tuple;
                    if (Operations.TryGetValue((T)itr.Current, out tuple) && kvp.Key == tuple.Item2)
                    {
                        Print.Log("doing operation", itr.Current);

                        Operator<T> operation = tuple.Item1;

                        IEditEnumerator<T>[] operandItrs = new IEditEnumerator<T>[operation.Targets.Length];
                        for (int j = 0; j < operation.Targets.Length; j++)
                        {
                            //operation.Targets[j](operandItrs[j] = itr.Copy());
                        }

                        T[] operands = new T[operandItrs.Length];
                        for (int j = 0; j < operandItrs.Length; j++)
                        {
                            Print.Log("\t" + operandItrs[j].Current);
                            operands[j] = ParseOperand(operandItrs[j]);
                            operandItrs[j].Remove(0);
                        }

                        itr.Add(0, operation.Operate(operands));
                    }
                }
            }

#if DEBUG
            Print.Log("done");
            IEditEnumerator<T> printer = start.Copy();
            while (printer.MoveNext())
            {
                Print.Log("\t" + printer.Current);
            }
#endif

            IEditEnumerator<T> juxtapose = start.Copy();
            T result = Juxtapose(CollectOperands(juxtapose, direction));
            juxtapose.Remove(0);
            return result;
        }
    }
}
#endif