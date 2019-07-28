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
    //using TargetFunction = Func<LinkedListNode<object>, LinkedListNode<object>>;
    using TargetFunction = Action<IEditEnumerator<object>>;

    public delegate T FunctionWithVariableParamterCount<T>(params T[] operands);
    public enum ProcessingOrder { LeftToRight = 1, RightToLeft = -1 }

    public class Operator<T>
    {
        public FunctionWithVariableParamterCount<T> Operate;
        public TargetFunction[] Targets;
        public ProcessingOrder Order;

        public Operator(FunctionWithVariableParamterCount<T> operate, params TargetFunction[] targets)
        {
            Operate = operate;
            Targets = targets;
            Order = ProcessingOrder.LeftToRight;
        }

        public Operator(FunctionWithVariableParamterCount<T> operate, ProcessingOrder order, params TargetFunction[] targets) : this(operate, targets)
        {
            Order = order;
        }
    }

    public class BinaryOperator<T> : Operator<T>
    {
        //public BinaryOperator(Func<T, T, T> func, TargetFunction previous, TargetFunction next) : base((o) => func(o[0], o[1]), previous, next) { }

        public BinaryOperator(Func<T, T, T> func, TargetFunction previous, TargetFunction next, ProcessingOrder order = ProcessingOrder.LeftToRight) : base((o) => func(o[0], o[1]), order, previous, next) { }
    }

    public class UnaryOperator<T> : Operator<T> { public UnaryOperator(Func<T, T> func, TargetFunction operand) : base((o) => func(o[0]), ProcessingOrder.RightToLeft, operand) { } }
}