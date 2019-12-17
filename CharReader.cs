using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Extensions;
//using Crunch.Machine;

namespace Parse
{
    /*public abstract class CharReader<TOutput> : Reader<string, TOutput>
    {
        new public Trie<Tuple<Operator<TOutput>, int>> Operations => (Trie<Tuple<Operator<TOutput>, int>>)base.Operations;

        public CharReader(params KeyValuePair<string, Operator<TOutput>>[][] operations) : base(operations.Flatten(new Trie<Tuple<Operator<TOutput>, int>>()))
        {
            Opening = new HashSet<string> { "(", "{", "[" };
            Closing = new HashSet<string> { ")", "}", "]" };
            Ignore = new HashSet<string> { " " };
        }

        public TOutput Parse(string input) => Parse(Next(input));

        protected virtual IEnumerable<string> Segment(IEnumerable<char> pieces)
        {
            foreach (char c in pieces)
            {
                yield return c.ToString();
            }
        }

        protected IEnumerable<string> Next(string input)
        {
            Lexer lexer = new Lexer();
            lexer
        }
    }*/

    public abstract class CharReader : Reader<Token>
    {
        public Trie<Tuple<Operator<Token>, int>> Operations;// => (Trie<Tuple<Operator<TOutput>, int>>)base.Operations;
        public HashSet<string> Opening;
        public HashSet<string> Closing;
        public HashSet<string> Ignore;

        public CharReader(params KeyValuePair<string, Operator<Token>>[][] operations)// : base(operations.Flatten(new Trie<Tuple<Operator<TOutput>, int>>()))
        {
            Operations = new Trie<Tuple<Operator<Token>, int>>();
            for (int i = 0; i < operations.Length; i++)
            {
                foreach (KeyValuePair<string, Operator<Token>> kvp in operations[i])
                {
                    //yield return new KeyValuePair<TKey, Tuple<TValue, int>>(kvp.Key, new Tuple<TValue, int>(kvp.Value, i));
                    Operations.Add(kvp.Key, new Tuple<Operator<Token>, int>(kvp.Value, i));
                }
            }

            Opening = new HashSet<string> { "(", "{", "[" };
            Closing = new HashSet<string> { ")", "}", "]" };
            Ignore = new HashSet<string> { " " };
        }

        /*private Trie<TKey, Tuple<TValue, int>> Flatten<TKey, TValue>(this IEnumerable<KeyValuePair<TKey, TValue>>[] operations, IDictionary<TKey, Tuple<TValue, int>> dict)
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
        }*/

        /*public CharReader(Trie<Tuple<Operator<TOutput>, int>> operations) : base(operations)
        {
            Opening = new HashSet<string> { "(", "{", "[" };
            Closing = new HashSet<string> { ")", "}", "]" };
            Ignore = new HashSet<string> { " " };
        }*/

        protected override Token ParseOperand(object operand)
        {
            return (Token)operand;
            //return (Token)(operand as Token).Value;
            //Token token = (Token)operand;
            //return token.Value is TOutput ? (TOutput)token.Value : ParseString(token.Value as string);
        }

        protected abstract Token ParseString(string input);

        public override Token Classify(object input)
        {
            //if (input is OperatorToken<TOutput> || input is OperandToken<TOutput>)
            if (input is Token && input.GetType() != typeof(Token))
            {
                return input as Token;
            }

            input = (input as Token)?.Value ?? input;

            if (input is string tInput)
            {
                if (Opening.Contains(tInput))
                {
                    return new Token { Class = Member.Opening };
                }
                else if (Closing.Contains(tInput))
                {
                    return new Token { Class = Member.Closing };
                }
                else if (Operations.ContainsKey(tInput))
                {
                    return new Token { Class = Member.Operator };
                }
            }

            return new Token { Class = Member.Operand };
        }

        /*public override Token Classify(object input)
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

            if (input is string tInput)
            {
                if (Opening.Contains(tInput))
                {
                    return Member.Opening;
                }
                else if (Closing.Contains(tInput))
                {
                    return Member.Closing;
                }
                else if (Operations.ContainsKey(tInput))
                {
                    return Member.Operator;
                }
            }

            return Member.Operand;
        }*/

        private string buffer1 = "";
        private string buffer2 = "";
        private Operator<Token> lastOperation = null;
        private Operator<Token> temp = null;

        public Token Parse(string input)
        {
            return ParseTest(input);
        }

        public Token ParseTest(string input)
        {
            Lexer<string, Token> lexer = new Lexer<string, Token>(Operations, Tokenize);

            Collections.Generic.LinkedList<Token> list = new Collections.Generic.LinkedList<Token>();
            foreach (Token t in lexer.TokenStream(input))
            {
                list.AddLast(t);
            }

            return (Token)Parse(list);
            //return (TOutput)((Token)Parse(list)).Value;
        }

        private IEnumerable<Token> Tokenize(IEnumerable<char> pieces)
        {
            foreach (string s in Segment(pieces))
            {
                yield return ParseString(s);// new OperandToken<int> { Value = ParseString(s) };
            }
        }

        protected virtual IEnumerable<string> Segment(IEnumerable<char> pieces)
        {
            foreach(char c in pieces)
            {
                yield return c.ToString();
            }
        }

        protected IEnumerable<string> Next(string input)
        {
            for (int i = 0; i < input.Length || buffer1.Length > 0 || buffer2.Length > 0; i++)
            {
                TrieContains search;

                //while (i < input.Length && Ignore.Contains(input[i].ToString())) { i++; }

                if (i >= input.Length || (int)Classify(input[i].ToString()).Class < 2)
                {
                    // We have emptied both buffers, so we're done with everything up to this point
                    if (buffer1.Length == 0 && buffer2.Length == 0 && i < input.Length)
                    {
                        yield return input[i].ToString();
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
                else
                {
                    buffer2 += input[i];
                    Tuple<Operator<Token>, int> tuple;

                    // If we haven't found an operator yet, ignore what we have (any partial matches are in buffer2)
                    // Otherwise we're storing the matched part of the operator in buffer1 
                    search = Operations.TryGetValue1((lastOperation == null ? "" : buffer1) + buffer2, out tuple);
                    temp = tuple?.Item1;
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
                        yield return buffer1;
                    }
                    // Operand
                    else
                    {
                        foreach (string o in Segment(buffer1))
                        {
                            yield return o;
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