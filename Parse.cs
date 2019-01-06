using System;
using System.Collections.Generic;
using System.Text;

using System.Extensions;

namespace Crunch.Machine
{
    using Evaluator = Tuple<Quantity, LinkedList<Node<object>>[]>;

    public static class Parse
    {
        public static readonly HashSet<char> Reserved = new HashSet<char>() { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', '+', '-', '*', '×', '/', '(', ')', '.' };

        public static bool IsEqualTo<T>(this Node<T> node, string str) => node != null && node.Value is string && node.Value.ToString() == str;

        /// <summary>
        /// Removes the first node of the LinkedList and returns it. Returns null if the list is empty
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
        /// <returns></returns>
        public static LinkedListNode<T> Dequeue<T>(this LinkedList<T> list)
        {
            LinkedListNode<T> temp = list.First;
            if (list.Count > 0)
            {
                list.RemoveFirst();
            }
            return temp;
        }

        /// <summary>
        /// Removes the last node of the LinkedList and returns it. Returns null if the list is empty
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
        /// <returns></returns>
        public static LinkedListNode<T> op<T>(this LinkedList<T> list)
        {
            LinkedListNode<T> temp = list.Last;
            if (list.Count > 0)
            {
                list.RemoveLast();
            }
            return temp;
        }

        /// <summary>
        /// Parse math
        /// </summary>
        /// <param name="str"><summary>string to be parsed</summary></param>
        /// <param name="operations"><summary>operations to execute. Add strings that should be recognized as operations, and the corresponding operation that should occur</summary></param>
        /// <param name="negate">include if you would like negative signs (ie 6+-4) to be recognized</param>
        /// <returns></returns>
        public static Quantity Math(string str, OrderedTrie<Operator> operations, Func<object, object> negate = null)
        {
            Operator negator = new Operator((o) => negate(o[0]), Node<object>.NextNode);
            Stack<Evaluator> quantities = new Stack<Evaluator>();

            Operator juxtapose;
            operations.Contains("*", out juxtapose);
            int multiplication = operations.IndexOf("*");

            str = "(" + str + ")";

            for (int i = 0; i < str.Length; i++)
            {
                char c = str[i];
                
                if (c == ' ')
                {
                    continue;
                }

                if (c.IsOpening())
                {
                    quantities.Push(new Evaluator(new Quantity(), new LinkedList<Node<object>>[operations.Count]));
                }
                else if (c.IsClosing())
                {
                    Evaluator e = quantities.Pop();
                    
                    for (int j = 0; j < e.Item2.Length; j++)
                    {
                        LinkedList<Node<object>> stack = e.Item2[j];

                        while (stack?.Count > 0)
                        {
                            Node<object> node = stack.Dequeue().Value;
                            Operator op = (Operator)node.Value;
                            
                            Node<object>[] operandNodes = new Node<object>[op.Targets.Length];
                            
                            for (int k = 0; k < op.Targets.Length; k++)
                            {
                                Node<object> operand = op.Targets[k](node);

                                if (operand == node.Next && operand.IsEqualTo("-"))
                                {
                                    operand.Next.Value = negate(operand.Next.Value);
                                    e.Item1.Remove(operand);
                                    operand = node.Next;
                                }

                                operandNodes[k] = operand;
                            }

                            object[] operands = new object[operandNodes.Length];
                            for (int k = 0; k < operands.Length; k++)
                            {
                                operands[k] = e.Item1.Remove(operandNodes[k])?.Value;
                            }

                            node.Value = op.Operate(operands);
                        }

                        if (j == multiplication)
                        {
                            Node<object> node = e.Item1.First;

                            while (node != null && node.Next != null)
                            {
                                if (node.Value is Operator || node.IsEqualTo("-") || node.Next.Value is Operator || node.Next.IsEqualTo("-"))
                                {
                                    node = node.Next;
                                }
                                else
                                {
                                    node.Value = juxtapose.Operate(node.Value, node.Next.Value);
                                    e.Item1.Remove(node.Next);
                                }
                            }
                        }
                    }

                    if (quantities.Count == 0)
                    {
                        if (i + 1 == str.Length)
                        {
                            return e.Item1;
                        }
                        quantities.Push(new Evaluator(new Quantity(), new LinkedList<Node<object>>[operations.Count]));
                    }

                    quantities.Peek().Item1.AddLast(e.Item1);
                }
                else
                {
                    //Keep track of the end position of the longest possible operation we find
                    int operation = i;
                    Operator op = null;
                    for (int j = i; j < str.Length; j++)
                    {
                        Operator temp;
                        TrieContains search = operations.Contains(str.Substring(i, j - i + 1), out temp);
                        //At this point there is no operation that starts like this
                        if (search == TrieContains.No)
                        {
                            break;
                        }
                        //We found an operation, but it might not be the longest one
                        if (search == TrieContains.Full)
                        {
                            operation = j + 1;
                            op = temp;
                        }
                    }

                    string s = operation > i ? str.Substring(i, operation - i) : c.ToString();
                    Node<object> node = new Node<object>(s);
                    quantities.Peek().Item1.AddLast(node);

                    if (operation > i)
                    {
                        bool isNegativeSign = s == "-" && node.Previous != null && node.Previous.Value is Operator;

                        if (!isNegativeSign)
                        {
                            node.Value = s == "-" && node.Previous == null ? negator : op;

                            int index = operations.IndexOf(s);
                            
                            if (quantities.Peek().Item2[index] == null)
                            {
                                quantities.Peek().Item2[index] = new LinkedList<Node<object>>();
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
                        }
                        i = operation - 1;
                    }
                    else if (s.IsNumber())
                    {
                        while (i + 1 < str.Length && str[i + 1].ToString().IsNumber())
                        {
                            node.Value = node.Value.ToString() + str[++i];
                        }
                    }
                }

                if (i + 1 == str.Length)
                {
                    for (int j = 0; j < quantities.Count; j++)
                    {
                        str += ")";
                    }
                }
            }

            throw new Exception("Error parsing math");
        }
    }
}
