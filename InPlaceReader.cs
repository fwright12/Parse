using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Extensions;
using System.Collections;
//using Crunch.Machine;

#if !DEBUG
namespace Parse
{
    public class IBiListEnumerator<T> : IBiEnumerator<T>
    {
        public override T Current => List[Index];

        private IList<T> List;
        private int Index;

        public IBiListEnumerator(IList<T> list)
        {
            List = list;
            Reset();
        }

        public override void Dispose() { }

        public override bool MoveNext() => ++Index < List.Count;
        public override bool MovePrev() => --Index >= 0;

        public override void Reset()
        {
            Index = -1;
        }
    }

    public abstract class Reader<T> : Reader<T, T>
    {
        public T Parse(IBiEnumerator<T> itr)
        {
            T quantity = default;

            //IEnumerator<T> itr = Next(input).GetEnumerator();

            /*string parsing = "parsing |";
            foreach (T s in Next(input))
            {
                parsing += s + "|";
            }
            Print.Log(parsing);*/

            do
            {
                quantity = Parse(ref itr, quantity);
            }
            while (itr != null);

            return quantity;
        }

        private T Parse(ref IBiEnumerator<T> itr, T firstInList = default)
        {
            //Evaluator quantity = new Evaluator(list, new LinkedList<LinkedListNode<object>>[Count]);
            LinkedList<IBiEnumerator<T>>[] operations = new LinkedList<IBiEnumerator<T>>[Count];

            while (true)
            {
                if (itr == null || !itr.MoveNext())
                {
                    itr = null;
                }

                if (itr == null || Closing.Contains(itr.Current))
                //if (classified == Classification.Opening)
                {
                    var e = operations;
                    /*Print.Log("close", e.Input.Count);
                    LinkedListNode<object> a = e.Input.First;
                    while (a != null)
                    {
                        Print.Log(a.Value, a.Value?.GetType());
                        a = a.Next;
                    }
                    Print.Log("\n");*/

                    return Close(e);
                    //return e.Input.First.Value;
                }
                //else if (classified == Classification.Closing)
                else if (Opening.Contains(itr.Current))
                {
                    Print.Log("open");

                    quantity.Input.AddLast(Parse(ref itr));
                    //LinkedList<object> e = Parse(ref itr);
                    //quantity.Input.AddLast(Delist(e));
                }
                else
                {
                    T next = itr.Current;

                    Operator<T> operation;
                    if (Operations.TryGetValue(next, out operation))
                    {
                        Print.Log("found operator", next);

                        int index = IndexOf(next);

                        // Put the operator in the linked list as a node
                        LinkedListNode<object> node = new LinkedListNode<object>(operation);

                        // Get the list of all of this type of operator (e.g. all instances of "+")
                        if (quantity.Operations[index] == null)
                        {
                            quantity.Operations[index] = new LinkedList<LinkedListNode<object>>();
                        }
                        var input = quantity.Operations[index];

                        if (operation.Order == ProcessingOrder.RightToLeft)
                        {
                            input.AddFirst(node);
                        }
                        else
                        {
                            input.AddLast(node);
                        }

                        quantity.Input.AddLast(node);
                    }
                    else
                    {
                        Print.Log("found operand", next);

                        foreach (T o in ParseOperand(next))
                        {
                            Print.Log("\t" + o);
                            quantity.Input.AddLast(o);
                        }
                    }
                }
            }
        }

        private T Close(LinkedList<IBiEnumerator<T>>[] operations)
        {
            for (int j = 0; j < operations.Length; j++)
            {
                LinkedList<IBiEnumerator<T>> stack = operations[j];
                
                while (stack?.Count > 0)
                {
                    IBiEnumerator<T> node = stack.Dequeue().Value;
                    /*if (node.List == null)
                    {
                        continue;
                    }*/
                    Operator<T> op = (Operator<T>)node.Current;
                    
                    LinkedListNode<object>[] operandNodes = new LinkedListNode<object>[op.Targets.Length];

                    for (int k = 0; k < op.Targets.Length; k++)
                    {
                        operandNodes[k] = op.Targets[k](node);
                    }

                    T[] operands = new T[operandNodes.Length];
                    for (int k = 0; k < operands.Length; k++)
                    {
                        operands[k] = (T)operandNodes[k]?.Value;
                        if (operandNodes[k] != null)
                        {
                            e.Input.Remove(operandNodes[k]);
                        }
                    }

                    Print.Log("operating", op.GetType(), operands.Length);
                    foreach(object o in operands)
                    {
                        Print.Log(o, o.GetType());
                    }
                    Print.Log("done");

                    node.Value = op.Operate(operands);
                }
            }

            return Juxtapose(e.Input);
        }

        private class Evaluator
        {
            public LinkedList<object> Input;
            public LinkedList<LinkedListNode<object>>[] Operations;

            public Evaluator(LinkedList<object> input, LinkedList<LinkedListNode<object>>[] operations)
            {
                Input = input;
                Operations = operations;
            }
        }
    }
}
#endif