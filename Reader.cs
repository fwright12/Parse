using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Extensions;

namespace Crunch.Machine
{
    using Evaluator = Tuple<LinkedList<object>, LinkedList<LinkedListNode<object>>[]>;

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
                for (int j = 0; j < data[i].Length; j++)
                {
                    Insert(i, data[i][j].Key, data[i][j].Value);
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

        public virtual LinkedList<object> Parse(string input)
        {
            print.log("parsing " + input);
            Stack<Evaluator> quantities = new Stack<Evaluator>();

            Operator juxtapose;
            Operations.Contains("*", out juxtapose);
            int multiplication = IndexOf("*");

            input = "(" + input + ")";

            for (int i = 0; i < input.Length; i++)
            {
                char c = input[i];

                if (c == ' ')
                {
                    continue;
                }

                if (c.IsOpening())
                {
                    quantities.Push(new Evaluator(new LinkedList<object>(), new LinkedList<LinkedListNode<object>>[Count]));
                }
                else if (c.IsClosing())
                {
                    Evaluator e = quantities.Pop();

                    Close(e, multiplication, juxtapose);

                    if (quantities.Count == 0)
                    {
                        if (i + 1 == input.Length)
                        {
                            return e.Item1;
                        }
                        quantities.Push(new Evaluator(new LinkedList<object>(), new LinkedList<LinkedListNode<object>>[Count]));
                    }

                    quantities.Peek().Item1.AddLast(e.Item1);
                }
                else
                {
                    string s = Search(input.Substring(i));
                    int index = IndexOf(s);

                    LinkedListNode<object> node = new LinkedListNode<object>(s);

                    if (index != -1)
                    {
                        node = new LinkedListNode<object>(Operations[s]);

                        if (quantities.Peek().Item2[index] == null)
                        {
                            quantities.Peek().Item2[index] = new LinkedList<LinkedListNode<object>>();
                        }
                        var list = quantities.Peek().Item2[index];

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

                        i += s.Length - 1;
                    }
                    else if (c.ToString().IsNumber())
                    {
                        while (i + 1 < input.Length && input[i + 1].ToString().IsNumber())
                        {
                            node.Value = node.Value.ToString() + input[++i];
                        }
                    }

                    quantities.Peek().Item1.AddLast(node);
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

        private void Close(Evaluator e, int multiplication, Operator juxtapose)
        {
            for (int j = 0; j < e.Item2.Length; j++)
            {
                LinkedList<LinkedListNode<object>> stack = e.Item2[j];

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
                            e.Item1.Remove(operandNodes[k]);
                        }
                    }

                    node.Value = op.Operate(operands);
                }

                if (j == multiplication)
                {
                    Juxtapose(e.Item1, juxtapose);
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
