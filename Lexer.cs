using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Extensions;

namespace Parse
{
    public class Token
    {
        public object Value;

        public virtual bool IsOperand => false;
        public virtual bool IsOperator => false;

        public override string ToString()
        {
            if (Value is Token)
            {
                return "Token";
            }
            return Value.ToString();
        }

        public bool IsOpeningToken => this is Separator separator && separator.IsOpening;

        public class Separator : Token
        {
            public bool IsOpening;
        }

        public class Operator<T> : Token
        {
            public Parse.Operator<T> Operation;
            public int Rank;

            public override bool IsOperator => true;
        }

        public class Operand<T> : Token
        {
            public T Something;
            public override bool IsOperand => true;
        }
    }

    public class Lexer<TOutput>
    {
        private Trie<Tuple<Operator<TOutput>, int>> Operations;

        //private Trie<object> Operators;
        private HashSet<char> Opening;
        private HashSet<char> Closing;
        private HashSet<char> Ignored;
        private Func<IEnumerable<char>, IEnumerable<Token>> Segment;
        //private Trie<Token> Classifier;

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

        /*public CharReader(params KeyValuePair<string, Operator<TOutput>>[][] operations) : base(operations.Flatten(new Trie<Tuple<Operator<TOutput>, int>>()))
        {
            Opening = new HashSet<string> { "(", "{", "[" };
            Closing = new HashSet<string> { ")", "}", "]" };
            Ignore = new HashSet<string> { " " };
        }*/

        /*public CharReader(Trie<Tuple<Operator<TOutput>, int>> operations) : base(operations)
        {
            Opening = new HashSet<string> { "(", "{", "[" };
            Closing = new HashSet<string> { ")", "}", "]" };
            Ignore = new HashSet<string> { " " };
        }*/

        private string buffer1 = "";
        private string buffer2 = "";
        private Token.Operator<TOutput> lastOperation = null;
        private Token.Operator<TOutput> temp = null;

        //public TOutput Parse(string input) => Parse(Next(input));

        /*protected virtual IEnumerable<Token> Segment(IEnumerable<char> pieces)
        {
            foreach (char c in pieces)
            {
                yield return new OperandToken<TOutput> { Value = c };
            }
        }*/

        public IEnumerable<Token> TokenStream(string input)
        {
            for (int i = 0; i < input.Length || buffer1.Length > 0 || buffer2.Length > 0; i++)
            {
                TrieContains search;
                
                //while (i < input.Length && Ignore.Contains(input[i].ToString())) { i++; }

                if (i >= input.Length || Opening.Contains(input[i]) || Closing.Contains(input[i]))
                {
                    // We have emptied both buffers, so we're done with everything up to this point
                    if (buffer1.Length == 0 && buffer2.Length == 0 && i < input.Length)
                    {
                        char c = input[i];
                        yield return new Token.Separator
                        {
                            Value = c.ToString(),
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
                        temp = new Token.Operator<TOutput>
                        {
                            Value = key,
                            Operation = tuple.Item1,
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
