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
        //public int[] Targets;
        public Func<Node<object>, Node<object>>[] Targets;
        //public Operator(Operation operate, params int[] targets) { Operate = operate; Targets = targets; }
        public Operator(FunctionWithVariableParamterCount operate, params Func<Node<object>, Node<object>>[] targets) { Operate = operate; Targets = targets; }
    }

    public class BinaryOperator : Operator { public BinaryOperator(Func<object, object, object> func) : base((o) => func(o[0], o[1]), Node<object>.PreviousNode, Node<object>.NextNode) { } }

    public class UnaryOperator : Operator { public UnaryOperator(Func<object, object> func) : base((o) => func(o[0]), Node<object>.NextNode) { } }
}
