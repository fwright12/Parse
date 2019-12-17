using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Extensions;
using System.Collections;

namespace Parse
{
    public enum Member { Opening = -1, Closing = 1, Operand, Operator };

    public abstract class Reader<TOutput>
    {
        /*public HashSet<TInput> Opening;
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
                }
            }
        }*/

        protected abstract TOutput ParseOperand(object operand);
        public TOutput ParseOperand<T>(IEditEnumerator<T> itr)
        {
            Member member = Classify(itr.Current).Class;
            if (member == Member.Opening || member == Member.Closing)
            {
                itr.Add(0, Parse((dynamic)itr, (ProcessingOrder)(-(int)member)));
            }
            else if (member == Member.Operator)
            {
                throw new Exception("Invalid syntax. Looking for operand but got operator");
            }

            return ParseOperand(itr.Current);
            object current = itr.Current;
            return current is TOutput ? (TOutput)current : ParseOperand(current);
        }

        protected virtual TOutput Juxtapose(IEnumerable<TOutput> expression) => expression.First();

        public IEnumerable<TOutput> CollectOperands<T>(IEditEnumerator<T> itr, ProcessingOrder direction = ProcessingOrder.LeftToRight) => CollectWhile(itr, (itr1) =>
        {
            Member member = Classify(itr1.Current).Class;
            return member != Member.Operator && (int)direction != (int)member;
        }, direction);

        public IEnumerable<TOutput> CollectWhile<T>(IEditEnumerator<T> itr, Func<IEditEnumerator<T>, bool> condition, ProcessingOrder direction = ProcessingOrder.LeftToRight)
        {
            bool empty = true;
            
            while (itr.Move((int)direction))
            {
                if (!condition(itr))
                {
                    break;
                }

                empty = false;

                yield return ParseOperand(itr);

                itr.Move(-(int)direction);
                itr.Remove((int)direction);
            }

            if (empty)
            {
                throw new Exception("No operands to collect");
            }
        }

        public abstract Token Classify(object input);

        public object Parse(IEditEnumerable<TOutput> input) => Parse(input.GetEnumerator());

        public object Parse(IEditEnumerator<TOutput> start, ProcessingOrder direction = ProcessingOrder.LeftToRight)
        {
            SortedDictionary<int, ProcessingOrder> order = new SortedDictionary<int, ProcessingOrder>();
            int count = 0;

#if DEBUG
            IEditEnumerator<TOutput> printer = start.Copy();

            string parsing = "";
            while (printer.Move((int)direction))
            {
                Print.Log(printer.Current);
                if (direction == ProcessingOrder.LeftToRight)
                {
                    parsing += printer.Current + "|";
                }
                else
                {
                    parsing = printer.Current + "|" + parsing;
                }
            }

            Print.Log("parsing section |" + parsing);
            Print.Log("start is " + start.Current + " and end is " + printer.Current);
#endif

            IEditEnumerator<TOutput> end = start.Copy();

            // Initial pass over the input to figure out:
            //      Where the beginning and end are (match parentheses)
            //      What operators we should look for (so we can skip iterating empty tiers)
            // Also delete anything that's supposed to be ignored
            while (end.Move((int)direction) && end.Current != null)
            {
                //Token token = (Token)end.Current;
                Token token = Classify(end.Current);
                
                // This is the "close" bracket for the direction we're moving
                if ((int)direction == (int)token.Class)
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
                else if ((int)direction == -(int)token.Class)
                {
                    count++;
                }
                // Keep track of what operators we find so we can skip them later
                else if (token is OperatorToken<TOutput> operatorToken)
                {
                    order[operatorToken.Rank] = operatorToken.Operator.Order;
                }
            }

            foreach (KeyValuePair<int, ProcessingOrder> kvp in order)
            {
                IEditEnumerator<TOutput> itr = kvp.Value == ProcessingOrder.LeftToRight ^ direction == ProcessingOrder.LeftToRight ? end.Copy() : start.Copy();
                
                while (itr.Move((int)kvp.Value) && !itr.Equals(start) && !itr.Equals(end) && itr.Current != null)
                {
                    if (!(Classify(itr.Current) is OperatorToken<TOutput> token))
                    {
                        continue;
                    }

                    if (kvp.Key == token.Rank)
                    //Operator<TOutput> operation;
                    //if (IsOperator((TInput)itr.Current, out operation))
                    {
                        Print.Log("doing operation", token.Value);

                        Operator<TOutput> operation = token.Operator;
                        itr.Add(0, default);
                        
                        IEditEnumerator<TOutput>[] operandItrs = new IEditEnumerator<TOutput>[operation.Targets.Length];
                        TOutput[] operands = new TOutput[operandItrs.Length];

                        for (int j = 0; j < operation.Targets.Length; j++)
                        {
                            operation.Targets[j](operandItrs[j] = itr.Copy());
                            operands[j] = ParseOperand(operandItrs[j]);
                            Print.Log("\t" + operandItrs[j].Current, (operandItrs[j].Current as Token).Value.GetType());
                        }

                        for (int j = 0; j < operandItrs.Length; j++)
                        {
                            operandItrs[j].Remove(0);
                        }

                        itr.Add(0, operation.Operate(operands));
                    }
                }
            }

#if DEBUG
            Print.Log("done");
            printer = start.Copy();
            while (printer.MoveNext())
            {
                Print.Log("\t" + printer.Current);
            }
#endif

            IEditEnumerator<TOutput> juxtapose = start.Copy();
            TOutput result = Juxtapose(CollectOperands(juxtapose, direction));
            juxtapose.Remove(0);
            return result;
        }
    }
}