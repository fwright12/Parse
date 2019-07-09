using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Extensions;
using Crunch.Machine;

namespace Parse
{
    public abstract class Reader<T>
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

        /*public LinkedList<object> Parse1(string input)
        {
            LinkedList<object> result = null;

            int i = -1;
            while (i < input.Length)
            {
                if (result != null)
                {
                    result = new LinkedList<object>(new object[] { result });
                }

                result = Parse1(ref input, ref i).Input;
            }

            return result;
        }*/

        public LinkedList<object> Parse(string input)
        {
            //return Parse1(input);

            Operator juxtapose;
            Operations.Contains("*", out juxtapose);
            int multiplication = IndexOf("*");

            LinkedList<object> quantity = null;

            for (int i = 0; i < input.Length; )
            {
                if (quantity != null)
                {
                    object o = Delist(quantity);
                    quantity = new LinkedList<object>();
                    quantity.AddFirst(o);
                }

                quantity = Parse(ref input, ref i, quantity);
            }

            return quantity;
        }

        private object Delist(LinkedList<object> list) => list.Count == 1 ? (object)list.First.Value : list;

        protected readonly int OPEN = char.MaxValue - 1;
        protected readonly int CLOSE = char.MaxValue - 2;

        protected virtual string Next(ref string input, ref int i)
        {
            do
            {
                if (i >= input.Length || input[i].IsClosing())
                {
                    return ((char)CLOSE).ToString();
                }
                else if (input[i].IsOpening())
                {
                    return ((char)OPEN).ToString();
                }
            }
            while (input[i++] == ' ');

            return input[i - 1].ToString();
        }

        private LinkedList<object> Parse(ref string input, ref int i, LinkedList<object> list = null)
        {
            Print.Log("parsing " + input);

            Operator juxtapose;
            Operations.Contains("*", out juxtapose);
            int multiplication = IndexOf("*");

            Evaluator<object> quantity = new Evaluator<object>(list ?? new LinkedList<object>(), new LinkedList<LinkedListNode<object>>[Count]);

            HashSet<char> terminaters = new HashSet<char>() { '(', ')' };
            HashSet<char> ignored = new HashSet<char>() { ' ' };
            
            while (true)
            {
                do
                {
                    string c = Next(ref input, ref i);

                    //if (i < input.Length && input[i].IsOpening())
                    if (c == OPEN.ToString())
                    {
                        Print.Log("open");

                        i++;
                        LinkedList<object> e = Parse(ref input, ref i);
                        quantity.Input.AddLast(Delist(e));
                    }
                    else if (c == CLOSE.ToString())
                    //else if (i >= input.Length || input[i].IsClosing())
                    {
                        Evaluator<object> e = quantity;
                        Print.Log("close", e.Input.Count);
                        LinkedListNode<object> a = e.Input.First;
                        while (a != null)
                        {
                            Print.Log(a.Value, a.Value?.GetType());
                            a = a.Next;
                        }
                        Print.Log("\n");

                        i++;
                        Close(e, multiplication, juxtapose);
                        return e.Input;
                    }
                    else
                    {
                        string next = c;

                        Operator temp1;
                        if (Operations.Contains(next, out temp1) == TrieContains.Full)
                        {
                            Print.Log("found operator", next);

                            int index = IndexOf(next);
                            LinkedListNode<object> node = AddOperator(quantity, temp1, index, next);
                            quantity.Input.AddLast(node);
                        }
                        else
                        {
                            Print.Log("found operand", next);
                            foreach (object o in ParseOperandString(next))
                            {
                                Print.Log("\t" + o);
                                quantity.Input.AddLast(o);
                            }
                        }
                    }
                }
                while (true);
            }
        }

        private LinkedList<object> Parse(ref List<string> input, ref int i)
        {
            string parsing = "parsing |";
            foreach (string s in input)
            {
                parsing += s + "|";
            }
            Print.Log(parsing);

            Evaluator<object> quantity = new Evaluator<object>(new LinkedList<object>(), new LinkedList<LinkedListNode<object>>[Count]);

            Operator juxtapose;
            Operations.Contains("*", out juxtapose);
            int multiplication = IndexOf("*");

            HashSet<char> terminaters = new HashSet<char>() { '(', ')' };
            HashSet<char> ignored = new HashSet<char>() { ' ' };

            string OPEN = "-1";
            string CLOSE = "-2";

            while (i <= input.Count)
            {
                string c = "";

                do
                {
                    if (i >= input.Count || input[i][0].IsClosing())
                    {
                        c = CLOSE;
                    }
                    else if (input[i][0].IsOpening())
                    {
                        c = OPEN;
                    }
                    else
                    {
                        c = input[i];
                    }

                    i++;
                }
                while (c[0] == ' ');

                Operator temp;
                if (Operations.Contains(c, out temp) == TrieContains.Full)
                {
                    // Operator

                    int index = IndexOf(c);
                    LinkedListNode<object> node = AddOperator(quantity, temp, index, c);
                    quantity.Input.AddLast(node);
                }
                else
                {
                    // Operand

                    foreach (object o in ParseOperandString(c))
                    {
                        Print.Log("\t" + o);
                        quantity.Input.AddLast(o);
                    }
                }

                if (c == OPEN)
                {
                    Print.Log("open");

                    LinkedList<object> e = Parse(ref input, ref i);
                    quantity.Input.AddLast(Delist(e));
                }
                else if (c == CLOSE)
                {
                    Evaluator<object> e = quantity;
                    Print.Log("close", e.Input.Count);
                    LinkedListNode<object> a = e.Input.First;
                    while (a != null)
                    {
                        Print.Log(a.Value, a.Value?.GetType());
                        a = a.Next;
                    }
                    Print.Log("\n");

                    Close(e, multiplication, juxtapose);
                    return e.Input;
                }
            }

            throw new Exception("Error parsing math");
        }

        private LinkedListNode<object> AddOperator(Evaluator<object> quantity, Operator o, int index, string s)
        {
            // Put the operator in the linked list as a node
            LinkedListNode<object> node = new LinkedListNode<object>(o);

            // Get the list of all of this type of operator (e.g. all instances of "+")
            if (quantity.Operations[index] == null)
            {
                quantity.Operations[index] = new LinkedList<LinkedListNode<object>>();
            }
            var list = quantity.Operations[index];

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

        private class Evaluator<T>
        {
            public LinkedList<T> Input;
            public LinkedList<LinkedListNode<T>>[] Operations;

            public Evaluator(LinkedList<T> input, LinkedList<LinkedListNode<T>>[] operations)
            {
                Input = input;
                Operations = operations;
            }
        }

        /*public LinkedList<object> Parse1(string input)
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

            for (int i = 0; i <= input.Length;)
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
                        LinkedListNode<object> node = AddOperator(quantities.Peek(), lastOperation, index, buffer1);
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
        }*/

        /*private LinkedList<object> Parse2(ref string input, ref int i, LinkedList<object> list = null)
        {
            Print.Log("parsing " + input);

            Operator juxtapose;
            Operations.Contains("*", out juxtapose);
            int multiplication = IndexOf("*");

            Evaluator<object> quantity = new Evaluator<object>(list ?? new LinkedList<object>(), new LinkedList<LinkedListNode<object>>[Count]);

            HashSet<char> terminaters = new HashSet<char>() { '(', ')' };
            HashSet<char> ignored = new HashSet<char>() { ' ' };

            string buffer1 = "";
            string buffer2 = "";
            Operator lastOperation = null;

            TrieContains search = TrieContains.Partial;
            Operator temp = null;

            //while (i <= input.Length)
            while (true)
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
                    //Print.Log(buffer2, i < input.Length ? input[i].ToString() : "i out of bounds");

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
                        LinkedListNode<object> node = AddOperator(quantity, lastOperation, index, buffer1);
                        quantity.Input.AddLast(node);

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
                        quantity.Input.AddLast(o);
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

                    LinkedList<object> e = Parse(ref input, ref i);
                    quantity.Input.AddLast(Delist(e));
                    //Close(e, multiplication, juxtapose);
                    //quantity.Input.AddLast(Delist(e.Input));
                }
                else if (c == CLOSE)
                {
                    Evaluator<object> e = quantity;
                    Print.Log("close", e.Input.Count);
                    LinkedListNode<object> a = e.Input.First;
                    while (a != null)
                    {
                        Print.Log(a.Value, a.Value?.GetType());
                        a = a.Next;
                    }
                    Print.Log("\n");

                    Close(e, multiplication, juxtapose);
                    return e.Input;
                }
            }

            //return quantity.Input;
            //throw new Exception("Error parsing math");
        }*/
    }
}
