using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Extensions;
using System.Collections;

namespace test
{
    public abstract class Reader<T> : Parse.Reader<T, T>
    {
        protected Reader(IDictionary<T, Tuple<Parse.Operator<T>, int>> operations) : base(operations)
        {

        }

        protected override T ParseOperand(T operand) => operand;
    }
}
namespace Parse
{
    public static class Extensions
    {
        public static IDictionary<TKey, Tuple<TValue, int>> Flatten<TKey, TValue>(this IEnumerable<KeyValuePair<TKey, TValue>>[] operations, IDictionary<TKey, Tuple<TValue, int>> dict)
        {
            dict.Clear();

            for (int i = 0; i < operations.Length; i++)
            {
                foreach (KeyValuePair<TKey, TValue> kvp in operations[i])
                {
                    //yield return new KeyValuePair<TKey, Tuple<TValue, int>>(kvp.Key, new Tuple<TValue, int>(kvp.Value, i));
                    dict.Add(kvp.Key, new Tuple<TValue, int>(kvp.Value, i));
                }
            }

            return dict;
        }
    }

    public enum Member { Opening = -1, Closing = 1, Operand, Operator };

    public abstract class Reader<T> : Reader<T, T>
    {
        protected Reader(IDictionary<T, Tuple<Operator<T>, int>> operations) : base(operations) { }

        protected override T ParseOperand(T operand) => operand;
    }

    public abstract class Reader<TInput, TOutput> : Reader1<TInput, TOutput>
    {
        protected Reader(IDictionary<TInput, Tuple<Operator<TOutput>, int>> operations) : base(operations)
        {

        }

        public override Member Classify(TInput input)
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
    }

    public class ReaderHelper<TReader, TInput, TOutput>
        where TReader : Reader1<TInput, TOutput>, new()
    {
        public readonly TReader[] Readers;

        protected ReaderHelper(KeyValuePair<TInput, Operator<TOutput>>[] operations)
        {

        }

        protected ReaderHelper(KeyValuePair<TInput, Operator<TOutput>>[][] operations)
            //where TReader : Reader1<TInput, TOutput>, new()
        {
            foreach (KeyValuePair<TInput, Operator<TOutput>>[] list in operations)
            {
                //Readers.Add(new ReaderHelper<TInput, TOutput>(list));
            }
        }
    }

    public abstract class Reader1<TInput, TOutput>
    {
        public HashSet<TInput> Opening;
        public HashSet<TInput> Closing;
        public HashSet<TInput> Ignore;

        public readonly IDictionary<TInput, Tuple<Operator<TOutput>, int>> Operations;
        private readonly int Tiers = 0;

        protected Reader1(IDictionary<TInput, Tuple<Operator<TOutput>, int>> operations)
        {
            Operations = operations;

            Opening = new HashSet<TInput>();
            Closing = new HashSet<TInput>();
            Ignore = new HashSet<TInput>();

            foreach (KeyValuePair<TInput, Tuple<Operator<TOutput>, int>> kvp in Operations)
            {
                Tiers = Math.Max(Tiers, kvp.Value.Item2 + 1);
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

        //protected abstract bool IsOpening(TInput input);
        //protected abstract bool IsClosing(TInput input);
        //protected abstract bool IsOperator(TInput input, out Operator<TOutput> operation);

        //protected abstract Action<IEditEnumerator<object>> DoSomething(IEditEnumerator<object> itr);

        protected abstract TOutput ParseOperand(TInput operand);
        public TOutput ParseOperand<T>(IEditEnumerator<T> itr)
        {
            Member member = Classify(itr.Current);
            if (member == Member.Opening || member == Member.Closing)
            {
                itr.Add(0, Parse((dynamic)itr, (ProcessingOrder)(-(int)member)));
            }
            else if (member == Member.Operator)
            {
                throw new Exception("Invalid syntax. Looking for operand but got operator");
            }

            object current = (itr.Current as Token)?.Value ?? itr.Current;
            return current is TOutput ? (TOutput)current : ParseOperand((TInput)current);
        }

        protected virtual TOutput Juxtapose(IEnumerable<TOutput> expression) => expression.First();

        public IEnumerable<TOutput> CollectOperands<T>(IEditEnumerator<T> itr, ProcessingOrder direction = ProcessingOrder.LeftToRight) => CollectWhile(itr, (itr1) =>
        {
            Member member = Classify(itr1.Current);
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

        public Member Classify(object input)
        {
            if (input is OperatorToken<TOutput>)
            {
                return Member.Operator;
            }
            else if (input is OperandToken<TOutput>)
            {
                return Member.Operand;
            }
            input = (input as Token)?.Value ?? input;
            return input is TOutput ? Member.Operand : Classify((TInput)input);
        }
        public abstract Member Classify(TInput input);

        public TOutput ParseInPlace(IEditEnumerable<object> input) => Parse1(input.GetEnumerator());

        public TOutput Parse(IEnumerable<Token> input)
        {
            Collections.Generic.LinkedList<object> list = new Collections.Generic.LinkedList<object>();
            foreach (Token t in input)
            {
                //Print.Log("\t" + t);
                list.AddLast(t);
            }
            return (TOutput)Parse(list.GetEnumerator()).Value;
        }

        public TOutput Parse(IEnumerable<TInput> input)
        {
            //return ParseTest(input);

            Collections.Generic.LinkedList<object> list = new Collections.Generic.LinkedList<object>();
            foreach(TInput t in input)
            {
                //Print.Log("\t" + t);
                list.AddLast(t);
            }
            return ParseInPlace(list);
            //return Parse(new EditEnumerator<TOutput>(list.GetEnumerator(), ParseOperand));
            //return Parse(list.GetEnumerator());
        }

        private bool ShouldOperate(TInput input, out Tuple<Operator<TOutput>, int> tuple)
        {
            return Operations.TryGetValue(input, out tuple);
        }

        public Token Parse(IEditEnumerator<object> start, ProcessingOrder direction = ProcessingOrder.LeftToRight)
        {
            SortedDictionary<int, ProcessingOrder> order = new SortedDictionary<int, ProcessingOrder>();
            int count = 0;

#if DEBUG
            IEditEnumerator<object> printer = start.Copy();

            string parsing = "";
            while (printer.Move((int)direction))
            {
                Print.Log(printer.Current);
                if (direction == ProcessingOrder.LeftToRight)
                {
                    parsing += (printer.Current as Token).Value + "|";
                }
                else
                {
                    parsing = (printer.Current as Token).Value + "|" + parsing;
                }
            }

            Print.Log("parsing section |" + parsing);
            Print.Log("start is " + (start.Current as Token)?.Value + " and end is " + (printer.Current as Token)?.Value);
#endif

            IEditEnumerator<object> end = start.Copy();

            // Initial pass over the input to figure out:
            //      Where the beginning and end are (match parentheses)
            //      What operators we should look for (so we can skip iterating empty tiers)
            // Also delete anything that's supposed to be ignored
            while (end.Move((int)direction) && end.Current != null)
            {
                Token token = (Token)end.Current;
                Member member = token.Value is string ? Classify(token.Value) : (Member)(-10);
                
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
                // Keep track of what operators we find so we can skip them later
                else if (token is OperatorToken<TOutput> operatorToken)
                {
                    order[operatorToken.Rank] = operatorToken.Operator.Order;
                }
            }

            foreach (KeyValuePair<int, ProcessingOrder> kvp in order)
            {
                IEditEnumerator<object> itr = kvp.Value == ProcessingOrder.LeftToRight ^ direction == ProcessingOrder.LeftToRight ? end.Copy() : start.Copy();
                
                while (itr.Move((int)kvp.Value) && !itr.Equals(start) && !itr.Equals(end) && itr.Current != null)
                {
                    if (!(itr.Current is OperatorToken<TOutput> token))
                    {
                        continue;
                    }

                    if (kvp.Key == token.Rank)
                    //Operator<TOutput> operation;
                    //if (IsOperator((TInput)itr.Current, out operation))
                    {
                        Print.Log("doing operation", token.Value);

                        Operator<TOutput> operation = token.Operator;
                        itr.Add(0, null);
                        
                        IEditEnumerator<object>[] operandItrs = new IEditEnumerator<object>[operation.Targets.Length];
                        TOutput[] operands = new TOutput[operandItrs.Length];

                        for (int j = 0; j < operation.Targets.Length; j++)
                        {
                            operation.Targets[j](operandItrs[j] = itr.Copy());
                            operands[j] = ParseOperand(operandItrs[j]);
                            Print.Log("\t" + operandItrs[j].Current);
                        }

                        for (int j = 0; j < operandItrs.Length; j++)
                        {
                            operandItrs[j].Remove(0);
                        }

                        itr.Add(0, new OperandToken<TOutput> { Value = operation.Operate(operands) });
                    }
                }
            }

#if DEBUG
            Print.Log("done");
            printer = start.Copy();
            while (printer.MoveNext())
            {
                Print.Log("\t" + (printer.Current as Token)?.Value);
            }
#endif

            IEditEnumerator<object> juxtapose = start.Copy();
            TOutput result = Juxtapose(CollectOperands(juxtapose, direction));
            juxtapose.Remove(0);
            return new OperandToken<TOutput> { Value = result };
        }

        private TOutput Parse1(IEditEnumerator<object> start, ProcessingOrder direction = ProcessingOrder.LeftToRight)
        {
            SortedDictionary<int, ProcessingOrder> order = new SortedDictionary<int, ProcessingOrder>();
            int count = 0;

#if DEBUG
            IEditEnumerator<object> printer = start.Copy();

            string parsing = "";
            while (printer.Move((int)direction))
            {
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

            IEditEnumerator<object> end = start.Copy();
            Dictionary<TInput, Operator<TOutput>>[] operations = new Dictionary<TInput, Operator<TOutput>>[Tiers];

            // Initial pass over the input to figure out:
            //      Where the beginning and end are (match parentheses)
            //      What operators we should look for (so we can skip iterating empty tiers)
            // Also delete anything that's supposed to be ignored
            while (end.Move((int)direction) && end.Current != null)
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
                // Delete anything that's supposed to be ignored
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

                while (itr.Move((int)kvp.Value) && !itr.Equals(start) && !itr.Equals(end) && itr.Current != null)
                {
                    if (!(itr.Current is TInput))
                    {
                        continue;
                    }

                    Tuple<Operator<TOutput>, int> tuple;
                    if (ShouldOperate((TInput)itr.Current, out tuple) && kvp.Key == tuple.Item2)
                    //Operator<TOutput> operation;
                    //if (IsOperator((TInput)itr.Current, out operation))
                    {
                        Print.Log("doing operation", itr.Current);

                        Operator<TOutput> operation = tuple.Item1;
                        itr.Add(0, null);

                        IEditEnumerator<object>[] operandItrs = new IEditEnumerator<object>[operation.Targets.Length];
                        TOutput[] operands = new TOutput[operandItrs.Length];

                        for (int j = 0; j < operation.Targets.Length; j++)
                        {
                            operation.Targets[j](operandItrs[j] = itr.Copy());
                            operands[j] = ParseOperand(operandItrs[j]);
                            Print.Log("\t" + operandItrs[j].Current);
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

            IEditEnumerator<object> juxtapose = start.Copy();
            TOutput result = Juxtapose(CollectOperands(juxtapose, direction));
            juxtapose.Remove(0);
            return result;
        }

        /*public class EditEnumerator<T> : IEditEnumerator<T>
        {
            private IEditEnumerator<object> Itr;
            private Func<IEditEnumerator<object>, T> Parse;

            public EditEnumerator(IEditEnumerator<object> itr, Func<IEditEnumerator<object>, T> parse)
            {
                Itr = itr;
                Parse = parse;
            }

            public T Current => Parse(Itr);
            object IEnumerator.Current => Itr.Current;

            public void Add(int n, T t) => Itr.Add(n, t);

            public IEditEnumerator<T> Copy() => new EditEnumerator<T>(Itr.Copy(), Parse);
            IEditEnumerator IEditEnumerator.Copy() => Itr.Copy();

            public void Dispose() => Itr.Dispose();

            public bool Move(int n) => Itr.Move(n);

            public bool MoveNext() => Itr.MoveNext();

            public bool MovePrev() => Itr.MovePrev();

            public bool Remove(int n) => Itr.Remove(n);

            public void Reset() => Itr.Reset();
        }*/
    }
}