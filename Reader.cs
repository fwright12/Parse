using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Extensions;
using Crunch.Machine;

namespace Parse
{
    //using Evaluator = Tuple<LinkedList<object>, LinkedList<LinkedListNode<object>>[]>;

    public class Evaluator<T>
    {
        public LinkedList<T> Input;
        public LinkedList<LinkedListNode<T>>[] Operations;

        public Evaluator(LinkedList<T> input, LinkedList<LinkedListNode<T>>[] operations)
        {
            Input = input;
            Operations = operations;
        }
    }

    public abstract class Reader
    {
        public int Count => dict.Count;

        protected Trie<Operator> Operations;

        private Dictionary<string, int> dict;

        public Reader(params KeyValuePair<string, Operator>[][] data)
        {
            Operations = new Trie<Operator>();
            dict = new Dictionary<string, int>();

            for (int i = 0; i < data.Length; i++)
            {
                foreach (KeyValuePair<string, Operator> kvp in data[i])
                //for (int j = 0; j < data[i].Length; j++)
                {
                    if (kvp.Key.Length == 1 && (kvp.Key[0].IsClosing() || kvp.Key[0].IsOpening()))
                    {
                        throw new Exception("The character " + kvp + " cannot appear in a command - this character is used to separate quantities");
                    }

                    Insert(i, kvp.Key, kvp.Value);
                }
            }
        }

        public void Add(string symbol, Operator operation) => Insert(dict.Count, symbol, operation);

        public void Insert(int index, string symbol, Operator operation)
        {
            KeyValuePair<string, Operator> pair = new KeyValuePair<string, Operator>(symbol, operation);
            dict.Add(pair.Key, index);
            Operations.Add(pair.Key, pair.Value);
        }

        private int IndexOf(string key) => dict.ContainsKey(key) ? dict[key] : -1;

        protected abstract IEnumerable<object> ParseOperandString(string operand);

        /*TrieContains search = TrieContains.Partial;

for (int i = 0; i < input.Length; i++)
{
    char c = input[i];

    while (search == TrieContains.Partial)
    {
        buffer2 += c;

        Operator temp;
        search = Operations.Contains((lastOperation != null ? buffer1 : "") + buffer2, out temp);
    }

    // Flush operand
    if (lastOperation == null && buffer1 != "")
    {
        Print.Log("found operand");
        foreach (object o in ParseOperandString(buffer1))
        {
            Print.Log(o);
            quantities.Peek().Input.AddLast(o);
        }
        Print.Log("done");

        buffer1 = "";
    }

    // Flush operation
    if (lastOperation != null)
    {
        Print.Log("found operation", buffer1, buffer2);

        int index = IndexOf(buffer1);
        LinkedListNode<object> node = AddOperator(quantities, lastOperation, index, buffer1);
        quantities.Peek().Input.AddLast(node);

        buffer1 = "";
        lastOperation = null;
        i -= buffer2.Length;
        c = ' ';
    }

    if (search == TrieContains.Full || c.IsOpening() || c.IsClosing())
    {
        if (search == TrieContains.Full)
        {
            buffer1 += buffer2;
            lastOperation = temp;
        }
    }

    if (search == TrieContains.No || c.IsOpening() || c.IsClosing())
    {
        if (search == TrieContains.No)
        {
            buffer1 += buffer2[0];
            i -= buffer2.Length - 1;

            if (!buffer2[0].IsOpening() && !buffer2[0].IsClosing())
            {
                c = ' ';
            }
        }
    }
}*/

        /*//primaryBuffer += c.ToString();

        Operator temp;
        TrieContains search = Operations.Contains(operationBuffer == null ? c.ToString() : primaryBuffer, out temp);

        if (search == TrieContains.Full || c.IsOpening() || c.IsClosing())
        {
            if (primaryBuffer != "")
            {
                Print.Log("found operand");
                foreach (object o in ParseOperandString(primaryBuffer))
                {
                    Print.Log(o);
                    quantities.Peek().Input.AddLast(o);
                }
                primaryBuffer = secondaryBuffer;
                Print.Log("done");
            }

            if (search == TrieContains.Full)
            {
                //operationBuffer += unknownBuffer;
                operationBuffer = temp;
            }
        }
        else if (search == TrieContains.No)
        {
            if (operationBuffer != null)
            {
                Print.Log("found operation");
                quantities.Peek().Input.AddLast(temp);

                operationBuffer = null;
                i -= primaryBuffer.Length;

                primaryBuffer = "";
                secondaryBuffer = "";
            }
        }

        primaryBuffer += c;*/

        public LinkedList<object> Parse(string input)
        /*{
            LinkedList<object> result = null;

            int i = -1;
            while (i < input.Length)
            {
                if (result != null)
                {
                    result = new LinkedList<object>(new object[] { result });
                }

                result = Parse(ref input, i);
            }

            return result;
        }

        public virtual LinkedList<object> Parse(ref string input, int i)*/
        {
            Print.Log("parsing " + input);
            Stack<Evaluator<object>> quantities = new Stack<Evaluator<object>>();

            Operator juxtapose;
            Operations.Contains("*", out juxtapose);
            int multiplication = IndexOf("*");

            //input = "(" + input;
            input = "(" + input + ")";

            HashSet<char> terminaters = new HashSet<char>() { '(', ')' };
            HashSet<char> ignored = new HashSet<char>() { ' ' };
            
            string buffer1 = "";
            string buffer2 = "";
            Operator lastOperation = null;

            TrieContains search = TrieContains.Partial;
            Operator temp = null;

            for (int i = 0; i <= input.Length; )
            {
                //int command = 0;
                int OPEN = -1;
                int CLOSE = -2;

                int c = -5;

                do
                {
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
                    //Print.Log(i < input.Length ? input[i].ToString() : "i out of bounds");

                    do
                    {
                        if (i >= input.Length)
                        {
                            c = CLOSE;
                        }
                        else
                        {
                            c = input[i];//.ToString();

                            if (((char)c).IsOpening())
                            {
                                c = OPEN;
                            }
                            else if (((char)c).IsClosing())
                            {
                                c = CLOSE;
                            }
                            else
                            {
                                i++;
                            }
                        }
                    }
                    while ((char)c == ' ');
                    
                    //command = 0;
                    //if (c[0].IsOpening() || c[0].IsClosing())
                    if (c < 0)
                    {
                        search = TrieContains.No;
                    }
                    else
                    {
                        buffer2 += (char)c;

                        // If we haven't found an operator yet, ignore what we have (any partial matches are in buffer2)
                        // Otherwise we're storing the matched part of the operator in buffer1 
                        search = Operations.Contains((lastOperation == null ? "" : buffer1) + buffer2, out temp);
                    }
                }
                // Break out of the loop when the following conditions are met
                //      If the search is not a partial match, we have reached a definite yes or no, so we need to do something
                //      If we have definitely found an operator, we may need to flush an operand
                //      If we found an operator and were looking for a longer one, we didn't find it - flush the operator we did find
                // If none of these are true, keep looping
                while (!(search != TrieContains.Partial && (search == TrieContains.Full || lastOperation != null || buffer2.Length == 0)));

                Print.Log("exited", buffer1, buffer2, search);

                // Flush operations unless we might still be looking

                if (search != TrieContains.Full)
                {
                    if (lastOperation != null)
                    {
                        Print.Log("found operation", buffer1, buffer2);

                        int index = IndexOf(buffer1);
                        LinkedListNode<object> node = AddOperator(quantities, lastOperation, index, buffer1);
                        quantities.Peek().Input.AddLast(node);

                        buffer1 = "";
                    }

                    if (buffer2.Length > 0)
                    {
                        continue;
                    }
                }

                if (!(search == TrieContains.No && lastOperation != null) && buffer1 != "")
                {
                    Print.Log("found operand");
                    foreach (object o in ParseOperandString(buffer1))
                    {
                        Print.Log("\t" + o);
                        quantities.Peek().Input.AddLast(o);
                    }

                    buffer1 = "";
                }

                if (c < 0)
                {
                    i++;
                }

                if (c == OPEN)
                {
                    Print.Log("open");
                    quantities.Push(new Evaluator<object>(new LinkedList<object>(), new LinkedList<LinkedListNode<object>>[Count]));
                }
                else if (c == CLOSE)
                {
                    Evaluator<object> e = quantities.Pop();
                    Print.Log("close", e.Input.Count);
                    LinkedListNode<object> a = e.Input.First;
                    while (a != null)
                    {
                        Print.Log(a.Value, a.Value?.GetType());
                        a = a.Next;
                    }
                    Print.Log("\n");

                    Close(e, multiplication, juxtapose);

                    if (quantities.Count == 0)
                    {
                        if (i + 1 >= input.Length)
                        {
                            return e.Input;
                        }
                        quantities.Push(new Evaluator<object>(new LinkedList<object>(), new LinkedList<LinkedListNode<object>>[Count]));
                    }

                    quantities.Peek().Input.AddLast(e.Input);
                }

                if (i + 1 == input.Length)
                {
                    for (int j = 0; j < quantities.Count; j++)
                    {
                        input += ")";
                    }
                }
            }

            throw new Exception("Error parsing math");
        }

        /*if (search != TrieContains.Full)
                    {
                        if (lastOperation != null)
                        {
                            Print.Log("found operation", buffer1, buffer2);

                            int index = IndexOf(buffer1);
                            LinkedListNode<object> node = AddOperator(quantities, lastOperation, index, buffer1);
                            quantities.Peek().Input.AddLast(node);

                            buffer1 = "";
                            lastOperation = null;
                            i -= buffer2.Length;
                            buffer2 = "";
                            continue;
                        }
                        else if (buffer2.Length > 1)
                        {
                            //buffer1 += buffer2[0];
                            i -= buffer2.Length - 1;
                            buffer2 = buffer2[0].ToString();
                            //buffer1 += buffer2;

                            continue;

                            /*if (!buffer2[0].IsOpening() && !buffer2[0].IsClosing())
                            {
                                //buffer1 += buffer2;
                                continue;
                            }
                        }
                    }

                    //if (search == TrieContains.Full || ((c.IsClosing() || c.IsOpening()) && buffer2 == c.ToString()))
                    if (search == TrieContains.Full || c.IsClosing() || c.IsOpening())
                    {
                        if (lastOperation == null && buffer1 != "")
                        {
                            Print.Log("found operand");
                            foreach (object o in ParseOperandString(buffer1))
                            {
                                Print.Log(o);
                                quantities.Peek().Input.AddLast(o);
                            }
                            Print.Log("done");

                            buffer1 = "";
                        }
                    }*/

        private LinkedListNode<object> AddOperator(Stack<Evaluator<object>> quantities, Operator o, int index, string s)
        {
            // Put the operator in the linked list as a node
            LinkedListNode<object> node = new LinkedListNode<object>(o);

            // Get the list of all of this type of operator (e.g. all instances of "+")
            if (quantities.Peek().Operations[index] == null)
            {
                quantities.Peek().Operations[index] = new LinkedList<LinkedListNode<object>>();
            }
            var list = quantities.Peek().Operations[index];

            //These operations are processed right to left
            if (s == "^" || s == "sin" || s == "cos" || s == "tan")
            {
                list.AddFirst(node);
            }
            //These are processed left to right
            else
            {
                list.AddLast(node);
            }

            return node;
        }

        private void Close(Evaluator<object> e, int multiplication, Operator juxtapose)
        {
            for (int j = 0; j < e.Operations.Length; j++)
            {
                LinkedList<LinkedListNode<object>> stack = e.Operations[j];
                
                while (stack?.Count > 0)
                {
                    LinkedListNode<object> node = stack.Dequeue().Value;
                    if (node.List == null)
                    {
                        continue;
                    }
                    Operator op = (Operator)node.Value;
                    
                    LinkedListNode<object>[] operandNodes = new LinkedListNode<object>[op.Targets.Length];

                    for (int k = 0; k < op.Targets.Length; k++)
                    {
                        operandNodes[k] = op.Targets[k](node);
                    }

                    object[] operands = new object[operandNodes.Length];
                    for (int k = 0; k < operands.Length; k++)
                    {
                        operands[k] = operandNodes[k]?.Value;
                        if (operandNodes[k] != null)
                        {
                            e.Input.Remove(operandNodes[k]);
                        }
                    }

                    Print.Log("operating", op.GetType());
                    foreach(object o in operands)
                    {
                        Print.Log(o, o.GetType());
                    }
                    Print.Log("done");

                    node.Value = op.Operate(operands);
                }

                if (j == multiplication)
                {
                    Juxtapose(e.Input, juxtapose);
                }
            }
        }

        private void Juxtapose(LinkedList<object> expression, Operator juxtapse)
        {
            LinkedListNode<object> node = expression.First;

            while (node.Next != null)
            {
                if (node.Value is Operator || node.Next.Value is Operator)
                {
                    node = node.Next;
                }
                else
                {
                    node.Value = juxtapse.Operate(node.Value, node.Next.Value);
                    expression.Remove(node.Next);
                }
            }
        }

        private string Search(string input)
        {
            //Keep track of the end position of the longest possible operation we find
            int operation = 0;
            for (int i = 0; i < input.Length; i++)
            {
                Operator temp;
                TrieContains search = Operations.Contains(input.Substring(0, i + 1), out temp);
                //At this point there is no operation that starts like this
                if (search == TrieContains.No)
                {
                    break;
                }
                //We found an operation, but it might not be the longest one
                if (search == TrieContains.Full)
                {
                    operation = i + 1;
                }
            }

            return input.Substring(0, Math.Max(1, operation));
        }
    }
}
