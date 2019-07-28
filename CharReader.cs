using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Extensions;
//using Crunch.Machine;

#if DEBUG
namespace Parse
{
    public abstract class Reader<TEnumerated, TInput, TOutput> : Reader<TInput, TOutput>
        where TInput : System.Collections.IEnumerable
    {
        public Reader(IDictionary<TInput, Tuple<Operator<TOutput>, int>> operations) : base(operations)
        {

        }

        protected abstract IEnumerable<TInput> Segment(IEnumerable<TEnumerated> pieces);
    }

    public abstract class CharReader<TOutput> : Reader<string, TOutput>
    {
        new public Trie<Tuple<Operator<TOutput>, int>> Operations => (Trie<Tuple<Operator<TOutput>, int>>)base.Operations;

        public CharReader(Trie<Tuple<Operator<TOutput>, int>> operations) : base(operations)
        {
            Opening = new HashSet<string> { "(", "{", "[" };
            Closing = new HashSet<string> { ")", "}", "]" };
            Ignore = new HashSet<string> { " " };
        }

        private string buffer1 = "";
        private string buffer2 = "";
        private Operator<TOutput> lastOperation = null;
        private Operator<TOutput> temp = null;

        public TOutput Parse(string input) => Parse(Next(input));

        protected virtual IEnumerable<string> Segment(IEnumerable<char> pieces)
        {
            foreach(char c in pieces)
            {
                yield return c.ToString();
            }
        }

        protected bool NextChar(ref string input, ref int i, out char next)
        {
            next = default;

            do
            {
                if (i >= input.Length)
                {
                    return true;
                }

                next = input[i];

                if (Closing.Contains(input[i].ToString()) || Opening.Contains(input[i].ToString()))
                {
                    return true;
                }
            }
            while (Ignore.Contains(input[i++].ToString()));

            next = input[i - 1];//.ToString();
            return false;
        }

        protected IEnumerable<object> Next(string input)
        {
            bool special = false;

            for (int i = 0; !(buffer1.Length == 0 && buffer2.Length == 0) || i < input.Length; )
            {
                TrieContains search;

                char next;
                special = NextChar(ref input, ref i, out next);

                //Print.Log(buffer2, i < input.Length ? input[i].ToString() : "i out of bounds");
                //Print.Log(search, buffer1, buffer2);

                if (special)
                {
                    // We have emptied both buffers, so we're done with everything up to this point
                    if (buffer1.Length == 0 && buffer2.Length == 0 && i++ < input.Length)
                    {
                        yield return next.ToString();
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
                    buffer2 += next;
                    Tuple<Operator<TOutput>, int> tuple;

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
#endif