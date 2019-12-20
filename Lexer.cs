using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Extensions;
using System.Collections;

namespace Parse
{
    public abstract class Token
    {
        public readonly object Value;

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

        public abstract class Operand : Token
        {
            public Operand(object value) : base(value) { }
        }

        public class Operand<T> : Operand
        {
            new public T Value => (T)base.Value;

            public Operand(T value) : base(value) { }

            public static implicit operator Operand<T>(T t) => new Operand<T>(t);
            public static implicit operator T(Operand<T> operand) => operand.Value;
        }

        public abstract class Operator : Token
        {
            public readonly int Rank;

            public Operator(object value, int rank) : base(value)
            {
                Rank = rank;
            }
        }

        public class Operator<T> : Operator
        {
            public readonly Parse.Operator<T> Operation;

            public Operator(object value, Parse.Operator<T> operation, int rank) : base(value, rank)
            {
                Operation = operation;

                /*Action<IEditEnumerator<T>>[] targets = new Action<IEditEnumerator<T>>[operation.Targets.Length];
                for (int i = 0; i < operation.Targets.Length; i++)
                {
                    int j = i;
                    targets[i] = (itr) => operation.Targets[j]((IEditEnumerator<Token<T>>)itr);
                }
                OtherOperation = new Operator<T>((o) =>
                {
                    Token<T>[] operands = new Token<T>[o.Length];
                    for (int i = 0; i < o.Length; i++)
                    {
                        operands[i] = new Token<T>.Operand(o[i]);
                    }
                    return (T)operation.Operate(operands).Value;
                }, operation.Order, targets);*/
            }
        }

        public class Separator : Token
        {
            public readonly bool IsOpening;

            public Separator(object value, bool isOpening) : base(value)
            {
                IsOpening = isOpening;
            }
        }
    }

    public class Lexer<T>
    {
        private Trie<Tuple<Operator<Token>, int>> Operations;

        private HashSet<char> Opening;
        private HashSet<char> Closing;
        private HashSet<char> Ignored;
        private Func<IEnumerable<char>, IEnumerable<Token.Operand<T>>> Segment;

        public Lexer(Trie<Tuple<Operator<Token>, int>> operations, Func<IEnumerable<char>, IEnumerable<Token.Operand<T>>> segment)
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
        private Token.Operator<Token> lastOperation = null;
        private Token.Operator<Token> temp = null;

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
                        yield return new Token.Separator(c.ToString(), Opening.Contains(c));
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
                    Tuple<Operator<Token>, int> tuple;

                    // If we haven't found an operator yet, ignore what we have (any partial matches are in buffer2)
                    // Otherwise we're storing the matched part of the operator in buffer1 
                    string key = (lastOperation == null ? "" : buffer1) + buffer2;
                    search = Operations.TryGetValue1(key, out tuple);
                    if (tuple != null)
                    {
                        temp = new Token.Operator<Token>(key, tuple.Item1, tuple.Item2);
                    }
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
                        foreach (Token.Operand<T> t in Segment(buffer1))
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
