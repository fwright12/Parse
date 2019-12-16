using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Crunch.Machine
{
    public static class StringClassification
    {
        public static bool IsOpening(this char c) => c == '(' || c == '{' || c == '[';
        public static bool IsClosing(this char c) => c == ')' || c == '}' || c == ']';
        public static bool IsOperator(this string s) => s.Length == 1 && (s == "/" || s == "×" || s == "+" || s == "*" || s == "-" || s == "^");
        public static bool IsNumber(this string s) => s.Length == 1 && ((s[0] >= 48 && s[0] <= 57) || s[0] == 46);

        public static string Simple(this string str)
        {
            str = str.Trim();
            switch (str)
            {
                case "÷": return "/";
                case "×": return "*";
                default: return str;
            }
        }
    }
}
