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

    public interface IClassifier<T>
    {
        Token Classify(T input);
    }

    public class Classifier<T> : IClassifier<T>
    {
        private readonly Func<T, Token> Implementation;

        public Classifier(Func<T, Token> implementation)
        {
            Implementation = implementation;
        }

        public Token Classify(T input) => Implementation(input);
    }

    public class Reader : Reader<Token>
    {
        public Reader(Func<IEnumerable<Token>, Token> juxtapose) : base(new Classifier<Token>((token) => token), juxtapose) { }
    }

    public class Reader<TOutput>
    {
        private readonly IClassifier<TOutput> Classifier;
        private readonly Func<IEnumerable<TOutput>, TOutput> Juxtapose;

        public Reader(IClassifier<TOutput> classifier, Func<IEnumerable<TOutput>, TOutput> juxtapose)
        {
            Classifier = classifier;
            Juxtapose = juxtapose;
        }

        public TOutput ParseOperand(IEditEnumerator<TOutput> itr)
        {
            Token token = Classifier.Classify(itr.Current);
            if (token is Token.Separator separator)
            {
                itr.Add(0, Parse(itr, separator.IsOpening ? ProcessingOrder.LeftToRight : ProcessingOrder.RightToLeft));
            }
            else if (token.GetType().Is(typeof(Token.Operator<>)))
            {
                throw new Exception("Invalid syntax. Looking for operand but got operator");
            }
            
            return itr.Current;
        }

        private bool IsOpening(Token.Separator token, ProcessingOrder direction) => (int)direction == token.IsOpening.ToInt() * 2 - 1;

        public IEnumerable<TOutput> CollectOperands(IEditEnumerator<TOutput> itr, ProcessingOrder direction = ProcessingOrder.LeftToRight) => CollectWhile(itr, (itr1) =>
        {
            Token token = Classifier.Classify(itr1.Current);
            return !token.GetType().Is(typeof(Token.Operator<>)) && (!(token is Token.Separator separator) ||IsOpening(separator, direction));
        }, direction);

        public IEnumerable<TOutput> CollectWhile(IEditEnumerator<TOutput> itr, Func<IEditEnumerator<TOutput>, bool> condition, ProcessingOrder direction = ProcessingOrder.LeftToRight)
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

        public TOutput Parse(IEnumerable<TOutput> input) => Parse(new Collections.Generic.LinkedList<TOutput>(input));

        public TOutput Parse(IEditEnumerable<TOutput> input) => Parse(input.GetEnumerator());

        public TOutput Parse(IEditEnumerator<TOutput> start, ProcessingOrder direction = ProcessingOrder.LeftToRight)
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
                Token token = Classifier.Classify(end.Current);

                if (token is Token.Separator separator)
                {
                    // This is the "open" bracket for the direction we're moving
                    if (IsOpening(separator, direction))
                    {
                        count++;
                    }
                    // This is the "close" bracket for the direction we're moving
                    else
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
                }
                // Keep track of what operators we find so we can skip them later
                else if (token is Token.Operator<TOutput> operatorToken)
                {
                    order[operatorToken.Rank] = operatorToken.Operation.Order;
                }
            }

            return Parse(order, start, end, direction);
        }

        private TOutput Parse(SortedDictionary<int, ProcessingOrder> order, IEditEnumerator<TOutput> start, IEditEnumerator<TOutput> end, ProcessingOrder direction = ProcessingOrder.LeftToRight)
        {
            foreach (KeyValuePair<int, ProcessingOrder> kvp in order)
            {
                IEditEnumerator<TOutput> itr = kvp.Value == ProcessingOrder.LeftToRight ^ direction == ProcessingOrder.LeftToRight ? end.Copy() : start.Copy();
                
                while (itr.Move((int)kvp.Value) && !itr.Equals(start) && !itr.Equals(end) && itr.Current != null)
                {
                    if (!(Classifier.Classify(itr.Current) is Token.Operator<TOutput> token))
                    {
                        continue;
                    }

                    if (kvp.Key == token.Rank)
                    //Operator<TOutput> operation;
                    //if (IsOperator((TInput)itr.Current, out operation))
                    {
                        Print.Log("doing operation", token.Value);

                        Operator<TOutput> operation = token.Operation;
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
            IEditEnumerator<TOutput> printer = start.Copy();
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