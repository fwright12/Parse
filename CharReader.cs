﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Extensions;
//using Crunch.Machine;

namespace Parse
{
    /*public abstract class CharReader
    {
        public Trie<Tuple<Operator<Token>, int>> Operations;

        protected abstract Token ParseString(string input);

        protected virtual Token Juxtapose(IEnumerable<Token> expression) => expression.First();

        protected virtual IEnumerable<string> Segment(IEnumerable<char> pieces)
        {
            foreach(char c in pieces)
            {
                yield return c.ToString();
            }
        }

        private string buffer1 = "";
        private string buffer2 = "";
        private Operator<Token> lastOperation = null;
        private Operator<Token> temp = null;

        protected IEnumerable<string> Next(string input)
        {
            for (int i = 0; i < input.Length || buffer1.Length > 0 || buffer2.Length > 0; i++)
            {
                TrieContains search;

                //while (i < input.Length && Ignore.Contains(input[i].ToString())) { i++; }

                if (i >= input.Length) // || (int)Classify(input[i].ToString()).Class < 2)
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
    }*/
}