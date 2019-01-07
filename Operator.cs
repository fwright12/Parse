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
        public Func<Node<object>, Node<object>>[] Targets;

        public Operator(FunctionWithVariableParamterCount operate, params Func<Node<object>, Node<object>>[] targets) { Operate = operate; Targets = targets; }
    }

    public class BinaryOperator : Operator { public BinaryOperator(Func<object, object, object> func, Func<Node<object>, Node<object>> previous, Func<Node<object>, Node<object>> next) : base((o) => func(o[0], o[1]), previous, next) { } }

    public class UnaryOperator : Operator { public UnaryOperator(Func<object, object> func, Func<Node<object>, Node<object>> operand) : base((o) => func(o[0]), operand) { } }
}
