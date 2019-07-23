﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Extensions;
//using Crunch.Machine;

#if DEBUG
namespace Parse
{
    public abstract class CharReader<TOutput> : Reader<string, TOutput>
    {
        new protected Trie<Operator<TOutput>> Operations => (Trie<Operator<TOutput>>)base.Operations;

        public CharReader(params KeyValuePair<string, Operator<TOutput>>[][] data) : base(new Trie<Operator<TOutput>>(), data)
        {
            Opening = new HashSet<string> { "(", "{", "[" };
            Closing = new HashSet<string> { ")", "}", "]" };
            Ignore = new HashSet<string> { " " };
        }

        private string buffer1 = "";
        private string buffer2 = "";
        private Operator<TOutput> lastOperation = null;
        private Operator<TOutput> temp = null;

        public TOutput Parse(string input) => Parse(Next1(input));

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

        protected System.BiEnumerable.LinkedList<string> Next1(string input)
        {
            System.BiEnumerable.LinkedList<string> list = new System.BiEnumerable.LinkedList<string>();
            foreach(string s in Next(input))
            {
                list.AddLast(s);
            }
            return list;
        }

        protected IEnumerable<string> Next(string input)
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

                    // If we haven't found an operator yet, ignore what we have (any partial matches are in buffer2)
                    // Otherwise we're storing the matched part of the operator in buffer1 
                    search = Operations.TryGetValue1((lastOperation == null ? "" : buffer1) + buffer2, out temp);
                }

                    // Need to flush an operator (no longer an operation, but came across one)
                if ((search == TrieContains.No && lastOperation != null) ||
                    // Need to flush an operand (found an operator, first one, something to flush)
                    (search == TrieContains.Full && lastOperation == null && buffer1 != ""))
                {
                    yield return buffer1;
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