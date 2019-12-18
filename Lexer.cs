using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Extensions;

namespace Parse
{
    public abstract partial class Token<T> : Token
    {
        public Token(object value) : base(value) { }

        public class Operand : Token<T>
        {
            public override bool IsOperand => true;

            public Operand(object value) : base(value) { }

            public static implicit operator T(Operand token) => (T)token.Value;
            public static implicit operator Operand(T t) => new Operand(t);
        }
    }

    public sealed class Token1
    {
        public class Separator : Token//<object>
        {
            public bool IsOpening;

            public Separator(object value) : base(value) { }
        }
    }

    public abstract partial class Token
    {
        public readonly object Value;

        public virtual bool IsOperand => false;
        public virtual bool IsOperator => false;

        public Token(object value)
        {
            Value = value;
        }

        public override string ToString()
        {
            if (Value is Token)
            {
                return "Token";
            }
            return Value.ToString();
        }

        public bool IsOpeningToken => this is Token1.Separator separator && separator.IsOpening;

        public abstract class Operator : Token
        {
            public int Rank;

            public abstract ProcessingOrder Order { get; }// => Operation.Order;

            private object Operation;

            public Operator(object value, object operation) : base(value)
            {
                Operation = operation;
            }

            public override bool IsOperator => true;

            public Action<IEditEnumerator<T>>[] GetTargets<T>() => GetOperation<T>().Targets;

            public T Operate<T>(params T[] o) => GetOperation<T>().Operate(o);

            private Parse.Operator<T> GetOperation<T>() => Operation as Parse.Operator<T> ?? throw new Exception("Cannot operate with values of type " + typeof(T));
        }

        public class Operator<T> : Operator
        {
            public override ProcessingOrder Order => Operation.Order;

            private Parse.Operator<T> Operation;

            public Operator(object value, Parse.Operator<T> operation) : base(value, operation)
            {
                Operation = operation;
            }
        }

        /*public abstract class AbstractOperator<T> : Token
        {
            public int Rank;
            public abstract ProcessingOrder Order { get; }
            public abstract Action<IEditEnumerator<T>>[] Targets { get; }

            public override bool IsOperator => true;

            public AbstractOperator(object value) : base(value) { }

            public abstract T Operate(params T[] o);
        }*/

        /*public class Operator<T1, T2> : AbstractOperator<T1>
        {
            private Parse.Operator<T2> Operation;
            private IConverter<T1, T2> Converter;

            public Operator(object value, Parse.Operator<T2> operation, IConverter<T1, T2> converter) : base(value)
            {
                Operation = operation;
                Converter = converter;
            }

            public override ProcessingOrder Order => Operation.Order;

            public override Action<IEditEnumerator<T1>>[] Targets
            {
                get
                {
                    throw new NotImplementedException();
                    //return Operation.Targets;
                }
            }

            public override T1 Operate(params T1[] t)
            {
                T2[] result = new T2[t.Length];
                for (int i = 0; i < t.Length; i++)
                {
                    result[i] = Converter.Convert(t[i]);
                }
                return Converter.Convert(Operation.Operate(result));
            }
        }*/
    }

    public class Lexer<TOutput>
    {
        private Trie<Tuple<Operator<TOutput>, int>> Operations;

        private HashSet<char> Opening;
        private HashSet<char> Closing;
        private HashSet<char> Ignored;
        private Func<IEnumerable<char>, IEnumerable<Token>> Segment;

        public Lexer(Trie<Tuple<Operator<TOutput>, int>> operations, Func<IEnumerable<char>, IEnumerable<Token>> segment)
        {
            Operations = operations;
            Segment = segment;
            Opening = new HashSet<char> { '(', '{', '[' };
            Closing = new HashSet<char> { ')', '}', ']' };
            Ignored = new HashSet<char> { ' ' };
            
            /*Classifier = new Trie<Token>
            {
                { "(", new Token { Name = Member.Opening } },
                { "{", new Token { Name = Member.Opening } },
                { "[", new Token { Name = Member.Opening } },
                { ")", new Token { Name = Member.Closing } },
                { "}", new Token { Name = Member.Closing } },
                { "]", new Token { Name = Member.Closing } }
            };*/
        }

        private string buffer1 = "";
        private string buffer2 = "";
        private Token.Operator<TOutput> lastOperation = null;
        private Token.Operator<TOutput> temp = null;

        public IEnumerable<Token> TokenStream(string input)
        {
            for (int i = 0; i < input.Length || buffer1.Length > 0 || buffer2.Length > 0; i++)
            {
                TrieContains search;

                if (i >= input.Length || Opening.Contains(input[i]) || Closing.Contains(input[i]))
                {
                    // We have emptied both buffers, so we're done with everything up to this point
                    if (buffer1.Length == 0 && buffer2.Length == 0 && i < input.Length)
                    {
                        char c = input[i];
                        yield return new Token1.Separator(c.ToString())
                        {
                            //Class = Opening.Contains(c) ? Member.Opening : Member.Closing,
                            IsOpening = Opening.Contains(c)
                        };
                    }
                    else
                    {
                        i--;
                    }

                    if (lastOperation != null || buffer2.Length > 0)
                    {
                        search = TrieContains.No;
                    }
                    else
                    {
                        search = TrieContains.Full;
                    }
                }
                else if (Ignored.Contains(input[i]))
                {
                    continue;
                }
                else
                {
                    buffer2 += input[i];
                    Tuple<Operator<TOutput>, int> tuple;

                    // If we haven't found an operator yet, ignore what we have (any partial matches are in buffer2)
                    // Otherwise we're storing the matched part of the operator in buffer1 
                    string key = (lastOperation == null ? "" : buffer1) + buffer2;
                    search = Operations.TryGetValue1(key, out tuple);
                    if (tuple != null)
                    {
                        temp = new Token.Operator<TOutput>(key, tuple.Item1)
                        {
                            Rank = tuple.Item2,
                        };
                    }
                    //temp = (Operator<TOutput>)evaluator.Evaluate(key);
                }

                if (buffer1.Length > 0 &&
                    // Need to flush an operator (no longer an operation, but came across one)
                    ((search == TrieContains.No && lastOperation != null) ||
                    // Need to flush an operand (found an operator, first one, something to flush)
                    (search == TrieContains.Full && lastOperation == null)))
                {
                    Print.Log("found", buffer1, search);
                    // Operator
                    if (search == TrieContains.No)
                    {
                        yield return lastOperation;
                    }
                    // Operand
                    else
                    {
                        foreach (Token t in Segment(buffer1))
                        {
                            yield return t;
                        }
                    }
                    buffer1 = "";
                }

                if (search == TrieContains.Full)
                {
                    buffer1 += buffer2;
                    lastOperation = temp;
                }
                else if (search == TrieContains.No)
                {
                    if (lastOperation != null)
                    {
                        lastOperation = null;
                    }
                    else if (buffer2.Length > 0)
                    {
                        buffer1 += buffer2[0].ToString();
                        i++;
                    }

                    i -= buffer2.Length;
                }

                if (search != TrieContains.Partial)
                {
                    buffer2 = "";
                }
            }
        }
    }
}
