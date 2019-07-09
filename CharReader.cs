using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Extensions;
using Crunch.Machine;

namespace Parse
{
    public abstract class Reader : Reader<char>
    {
        public Reader(params KeyValuePair<string, Operator>[][] data) : base(data) { }

        string buffer1 = "";
        string buffer2 = "";
        Operator lastOperation = null;

        TrieContains search = TrieContains.Partial;
        Operator temp = null;

        int c = -5;

        protected override string Next(ref string input, ref int i)
        {
            string next = "";
            //return next;
            do
            {
                c = base.Next(ref input, ref i)[0];

                bool done = false;


                //Print.Log(buffer2, i < input.Length ? input[i].ToString() : "i out of bounds");
                //Print.Log(search, buffer1, buffer2);


                if (c == OPEN || c == CLOSE)
                {
                    if (lastOperation != null || buffer2.Length > 0)
                    {
                        search = TrieContains.No;
                    }
                    else
                    {
                        search = TrieContains.Full;
                    }

                    /*if (lastOperation != null)
                    {
                        search = TrieContains.No;
                    }
                    else
                    {
                        search = TrieContains.Full;
                        if (buffer2.Length > 0)
                        {
                            lastOperation = new Operator(null, null, null);
                        }
                    }*/

                    // We have emptied both buffers, so we're done with everything up to this point
                    if (buffer1.Length == 0 && buffer2.Length == 0)
                    {
                        return c.ToString();
                    }
                    else
                    {
                        c = -5;
                    }
                }
                else
                {
                    buffer2 += (char)c;

                    // If we haven't found an operator yet, ignore what we have (any partial matches are in buffer2)
                    // Otherwise we're storing the matched part of the operator in buffer1 
                    search = Operations.Contains((lastOperation == null ? "" : buffer1) + buffer2, out temp);
                }
                /*}
                //while (true);
                // If any of the following are true, break out of the loop:
                while (!(
                // Need to flush an operand
                (search == TrieContains.Full && lastOperation == null && buffer1 != "") ||
                // Need to flush an operator
                (search == TrieContains.No && lastOperation != null) ||
                // (Opening or closing) and collected everything before
                ((c == OPEN || c == CLOSE) && buffer2.Length == 0)));*/

                //Print.Log("exited", buffer1, buffer2, search);

                //  Need to flush an operator (no longer an operation, but came across one)
                done = (search == TrieContains.No && lastOperation != null) ||
            // Need to flush an operand (found an operator, first one, something to flush)
            (search == TrieContains.Full && lastOperation == null && buffer1 != "");

                if (done)
                {
                    next = buffer1;
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

                if (done)
                {
                    break;
                }
            }
            while (true);

            return next;
        }
    }
}
