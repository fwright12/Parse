using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Extensions;
using System.Collections;

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

        protected abstract TOutput ParseOperand(TInput operand);
        public TOutput ParseOperand(IEditEnumerator<object> itr)
        {
            Member member = Classify(itr.Current);
            if (member == Member.Opening || member == Member.Closing)
            {
                itr.Add(0, Parse(itr, (ProcessingOrder)(-(int)member)));
            }

            return itr.Current is TOutput ? (TOutput)itr.Current : ParseOperand((TInput)itr.Current);
        }

        protected virtual TOutput Juxtapose(IEnumerable<TOutput> expression) => expression.First();

        public IEnumerable<TOutput> CollectOperands(IEditEnumerator<object> itr, ProcessingOrder direction = ProcessingOrder.LeftToRight)
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

        public TOutput Parse(IEnumerable<TInput> input)
        {
            System.BiEnumerable.LinkedList<object> list = new System.BiEnumerable.LinkedList<object>();
            foreach(TInput t in input)
            {
                //Print.Log("\t" + t);
                list.AddLast(t);
            }
            //return Parse(new EditEnumerator<TOutput>(list.GetEnumerator(), ParseOperand));
            return Parse(list.GetEnumerator());
        }

        private TOutput Parse(IEditEnumerator<object> start, ProcessingOrder direction = ProcessingOrder.LeftToRight)
        {
            SortedDictionary<int, ProcessingOrder> order = new SortedDictionary<int, ProcessingOrder>();
            IEditEnumerator<object> end = start.Copy();
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
                if (!(end.Current is TInput))
                {
                    continue;
                }

                TInput current = (TInput)end.Current;
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
                    Tuple<Operator<TOutput>, int> temp = Operations[current];
                    order[temp.Item2] = temp.Item1.Order;
                }
            }

            foreach (KeyValuePair<int, ProcessingOrder> kvp in order)
            {
                IEditEnumerator<object> itr = kvp.Value == ProcessingOrder.LeftToRight ^ direction == ProcessingOrder.LeftToRight ? end.Copy() : start.Copy();

                while (itr.Move((int)kvp.Value) && !itr.Equals(start) && !itr.Equals(end))
                {
                    if (!(itr.Current is TInput))
                    {
                        continue;
                    }

                    Tuple<Operator<TOutput>, int> tuple;
                    if (Operations.TryGetValue((TInput)itr.Current, out tuple) && kvp.Key == tuple.Item2)
                    {
                        Print.Log("doing operation", itr.Current);

                        Operator<TOutput> operation = tuple.Item1;

                        IEditEnumerator<object>[] operandItrs = new IEditEnumerator<object>[operation.Targets.Length];
                        for (int j = 0; j < operation.Targets.Length; j++)
                        {
                            operation.Targets[j](operandItrs[j] = itr.Copy());
                        }

                        TOutput[] operands = new TOutput[operandItrs.Length];
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
            IEditEnumerator<object> printer = start.Copy();
            while (printer.MoveNext())
            {
                Print.Log("\t" + printer.Current);
            }
#endif

            IEditEnumerator<object> juxtapose = start.Copy();
            TOutput result = Juxtapose(CollectOperands(juxtapose, direction));
            juxtapose.Remove(0);
            return result;
        }

        /*private TOutput Parse(EditEnumerator<TOutput> start, ProcessingOrder direction = ProcessingOrder.LeftToRight)
        {
            SortedDictionary<int, ProcessingOrder> order = new SortedDictionary<int, ProcessingOrder>();
            EditEnumerator<TOutput> end = (EditEnumerator<TOutput>)start.Copy();
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

            end = (EditEnumerator<TOutput>)start.Copy();
#endif

            // Initial pass over the input to figure out:
            //      Where the beginning and end are (match parentheses)
            //      What operators we should look for (so we can skip iterating empty tiers)
            // Also delete anything that's supposed to be ignored
            while (end.Move((int)direction))
            {
                if (!(end.Current is TInput))
                {
                    continue;
                }

                TInput current = (TInput)end.RawCurrent;
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
                    Tuple<Operator<TOutput>, int> temp = Operations[current];
                    order[temp.Item2] = temp.Item1.Order;
                }
            }

            foreach (KeyValuePair<int, ProcessingOrder> kvp in order)
            {
                EditEnumerator<TOutput> itr = kvp.Value == ProcessingOrder.LeftToRight ^ direction == ProcessingOrder.LeftToRight ? (EditEnumerator<TOutput>)end.Copy() : (EditEnumerator<TOutput>)start.Copy();

                while (itr.Move((int)kvp.Value) && !itr.Equals(start) && !itr.Equals(end))
                {
                    if (!(itr.Current is TInput))
                    {
                        continue;
                    }

                    Tuple<Operator<TOutput>, int> tuple;
                    if (Operations.TryGetValue((TInput)itr.RawCurrent, out tuple) && kvp.Key == tuple.Item2)
                    {
                        Print.Log("doing operation", itr.Current);

                        Operator<TOutput> operation = tuple.Item1;

                        EditEnumerator<TOutput>[] operandItrs = new EditEnumerator<TOutput>[operation.Targets.Length];
                        for (int j = 0; j < operation.Targets.Length; j++)
                        {
                            operation.Targets[j](operandItrs[j] = (EditEnumerator<TOutput>)itr.Copy());
                        }

                        TOutput[] operands = new TOutput[operandItrs.Length];
                        for (int j = 0; j < operandItrs.Length; j++)
                        {
                            Print.Log("\t" + operandItrs[j].Current);
                            operands[j] = operandItrs[j].Current;
                            operandItrs[j].Remove(0);
                        }

                        itr.Add(0, operation.Operate(operands));
                    }
                }
            }

#if DEBUG
            Print.Log("done");
            EditEnumerator<TOutput> printer = (EditEnumerator<TOutput>)start.Copy();
            while (printer.MoveNext())
            {
                Print.Log("\t" + printer.Current);
            }
#endif

            EditEnumerator<TOutput> juxtapose = (EditEnumerator<TOutput>)start.Copy();
            TOutput result = Juxtapose(CollectOperands(juxtapose, direction));
            juxtapose.Remove(0);
            return result;
        }*/

        /*public class Operator
        {
            private Operator<TOutput> Operation;
            private Action<IEditEnumerator<TOutput>>[] Targets;
            private int Rank;

            public Operator(Operator<TOutput> operation, int rank, params Action<IEditEnumerator<TOutput>>[] targets)
            {
                Operation = operation;
                Targets = targets;
                Rank = rank;
            }
        }*/

        /*public class EditEnumerator<T> : IEditEnumerator<T>
        {
            private IEditEnumerator<object> Itr;
            private Func<IEditEnumerator<object>, T> Parse;

            public EditEnumerator(IEditEnumerator<object> itr, Func<IEditEnumerator<object>, T> parse)
            {
                Itr = itr;
                Parse = parse;
            }

            public T CurrentOperand => Parse(Itr);
            public object RawCurrent => Itr.Current;

            T IEnumerator<T>.Current => CurrentOperand;
            object IEnumerator.Current => CurrentOperand;

            public void Add(int n, T t) => Itr.Add(n, t);

            public IEditEnumerator<T> Copy() => new EditEnumerator<T>(Itr.Copy(), Parse);

            public void Dispose() => Itr.Dispose();

            public bool Move(int n) => Itr.Move(n);

            public bool MoveNext() => Itr.MoveNext();

            public bool MovePrev() => Itr.MovePrev();

            public bool Remove(int n) => Itr.Remove(n);

            public void Reset() => Itr.Reset();
        }*/
    }
}
#endif