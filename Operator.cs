using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Crunch.Machine
{
    public delegate object FunctionWithVariableParamterCount(params object[] operands);

    public class Operator
    {
        public FunctionWithVariableParamterCount Operate;
        public Func<LinkedListNode<object>, LinkedListNode<object>>[] Targets;

        public Operator(FunctionWithVariableParamterCount operate, params Func<LinkedListNode<object>, LinkedListNode<object>>[] targets)
        {
            Operate = operate;
            Targets = targets;
        }
    }

    public class BinaryOperator : Operator
    {
        public BinaryOperator(Func<object, object, object> func, Func<LinkedListNode<object>, LinkedListNode<object>> previous, Func<LinkedListNode<object>, LinkedListNode<object>> next) : base((o) => func(o[0], o[1]), previous, next) { }
    }

    public class UnaryOperator : Operator { public UnaryOperator(Func<object, object> func, Func<LinkedListNode<object>, LinkedListNode<object>> operand) : base((o) => func(o[0]), operand) { } }
}

namespace Parse
{
    public delegate T FunctionWithVariableParamterCount<T>(params T[] operands);
    public delegate TOutput FunctionWithVariableParamterCount<TInput, TOutput>(params TInput[] operands);

    public enum ProcessingOrder { LeftToRight = 1, RightToLeft = -1 }

    public interface IOperator<T>
    {
        Action<IEditEnumerator<T>>[] Targets { get; }
        ProcessingOrder Order { get; }

        T Operate(params T[] operands);
    }

    /*public class Operator
    {
        public ProcessingOrder Order;

        //private Operator<object> TypedOperator;

        public FunctionWithVariableParamterCount<object> Operate { get; private set; }
        public Action<IEditEnumerator>[] Targets { get; private set; }

        //public ProcessingOrder Order;

        //public abstract Action<IEditEnumerator>[] DumbTargets { get; }

        private Operator()
        {
            //TypedOperator = typedOperator;
        }

        public static Operator Create<T>(Operator<T> operation)
        {
            Operator result = new Operator
            {
                Order = operation.Order
            };

            result.Targets = new Action<IEditEnumerator>[operation.Targets.Length];
            for (int i = 0; i < operation.Targets.Length; i++)
            {
                int j = i;
                result.Targets[i] = (itr) => operation.Targets[j]((IEditEnumerator<T>)itr);
            }

            result.Operate = new FunctionWithVariableParamterCount<object>((o) =>
            {
                T[] operands = new T[o.Length];
                for (int i = 0; i < o.Length; i++)
                {
                    operands[i] = (T)o[i];
                }
                return operation.Operate(operands);
            });

            return result;
        }
    }*/

    public class Operator<T>
    {
        public FunctionWithVariableParamterCount<T> OperateFunc;
        public Action<IEditEnumerator<T>>[] Targets;
        public ProcessingOrder Order;

        public Operator(FunctionWithVariableParamterCount<T> operateFunc, params Action<IEditEnumerator<T>>[] targets)
        {
            OperateFunc = operateFunc;
            Targets = targets;
            Order = ProcessingOrder.LeftToRight;
        }

        public Operator(FunctionWithVariableParamterCount<T> operate, ProcessingOrder order, params Action<IEditEnumerator<T>>[] targets) : this(operate, targets)
        {
            Order = order;
        }

        /*public Token<T> DoOperation(IEditEnumerator<Token<T>> itr)
        {
            IEditEnumerator<Token<T>>[] operandItrs = new IEditEnumerator<Token<T>>[Targets.Length];
            T[] operands = new T[operandItrs.Length];

            for (int j = 0; j < Targets.Length; j++)
            {
                //Targets[j](operandItrs[j] = itr.Copy());
                operands[j] = default;// ParseOperand(operandItrs[j]);
                Print.Log("\t" + operandItrs[j].Current);
            }

            for (int j = 0; j < operandItrs.Length; j++)
            {
                operandItrs[j].Remove(0);
            }

            return null; // Operate(operands);
        }*/
    }

    public class BinaryOperator<T> : Operator<T>
    {
        public BinaryOperator(Func<T, T, T> func, Action<IEditEnumerator<T>> previous, Action<IEditEnumerator<T>> next, ProcessingOrder order = ProcessingOrder.LeftToRight) : base((o) => func(o[0], o[1]), order, previous, next) { }
    }

    public class UnaryOperator<T> : Operator<T>
    {
        public UnaryOperator(Func<T, T> func, Action<IEditEnumerator<T>> operand) : base((o) => func(o[0]), ProcessingOrder.RightToLeft, operand) { }
    }
}

#if false
namespace Parse
{
    //using TargetFunction = Action<IEditEnumerator<T>>;
    using TokenType = Tuple<string, FunctionWithVariableParamterCount<string>>;

    public delegate T FunctionWithVariableParamterCount<T>(params T[] operands);
    public delegate TOutput FunctionWithVariableParamterCount<TInput, TOutput>(params TInput[] operands);
    
    public enum ProcessingOrder { LeftToRight = 1, RightToLeft = -1 }

    public class Operator<T> : Operator<T, T>
    {
        public Operator(FunctionWithVariableParamterCount<T> operate, params Action<IEditEnumerator<T>>[] targets) : base(operate, targets) { }

        public Operator(FunctionWithVariableParamterCount<T> operate, ProcessingOrder order, params Action<IEditEnumerator<T>>[] targets) : base(operate, order, targets) { }
    }

    public class Operator<TEnumerated, TOperated>
    {
        public FunctionWithVariableParamterCount<TOperated> Operate;
        public Action<IEditEnumerator<TEnumerated>>[] Targets;
        public ProcessingOrder Order;

        public Operator(FunctionWithVariableParamterCount<TOperated> operate, params Action<IEditEnumerator<TEnumerated>>[] targets)
        {
            Operate = operate;
            Targets = targets;
            Order = ProcessingOrder.LeftToRight;
        }

        public Operator(FunctionWithVariableParamterCount<TOperated> operate, ProcessingOrder order, params Action<IEditEnumerator<TEnumerated>>[] targets) : this(operate, targets)
        {
            Order = order;
        }

        /*public Token<T> DoOperation(IEditEnumerator<Token<T>> itr)
        {
            IEditEnumerator<Token<T>>[] operandItrs = new IEditEnumerator<Token<T>>[Targets.Length];
            T[] operands = new T[operandItrs.Length];

            for (int j = 0; j < Targets.Length; j++)
            {
                //Targets[j](operandItrs[j] = itr.Copy());
                operands[j] = default;// ParseOperand(operandItrs[j]);
                Print.Log("\t" + operandItrs[j].Current);
            }

            for (int j = 0; j < operandItrs.Length; j++)
            {
                operandItrs[j].Remove(0);
            }

            return null; // Operate(operands);
        }*/
    }

    public class BinaryOperator<T> : Operator<T>
    {
        public BinaryOperator(Func<T, T, T> func, Action<IEditEnumerator<T>> previous, Action<IEditEnumerator<T>> next, ProcessingOrder order = ProcessingOrder.LeftToRight) : base((o) => func(o[0], o[1]), order, previous, next) { }
    }

    public class BinaryOperator<TEnumerated, TOperated> : Operator<TEnumerated, TOperated>
    {
        //public BinaryOperator(Func<T, T, T> func, TargetFunction previous, TargetFunction next) : base((o) => func(o[0], o[1]), previous, next) { }

        public BinaryOperator(Func<TOperated, TOperated, TOperated> func, Action<IEditEnumerator<TEnumerated>> previous, Action<IEditEnumerator<TEnumerated>> next, ProcessingOrder order = ProcessingOrder.LeftToRight) : base((o) => func(o[0], o[1]), order, previous, next) { }
    }

    public class UnaryOperator<T> : Operator<T> { public UnaryOperator(Func<T, T> func, Action<IEditEnumerator<T>> operand) : base((o) => func(o[0]), ProcessingOrder.RightToLeft, operand) { } }

    public class UnaryOperator<TEnumerated, TOperated> : Operator<TEnumerated, TOperated> { public UnaryOperator(Func<TOperated, TOperated> func, Action<IEditEnumerator<TEnumerated>> operand) : base((o) => func(o[0]), ProcessingOrder.RightToLeft, operand) { } }
}
#endif