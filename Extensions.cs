using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Extensions;

namespace Parse
{
    public static class Extensions
    {
        public static Trie<Tuple<Operator<T>, int>> MakeTrie<T>(this KeyValuePair<string, Operator<T>>[][] operations)
        {
            var result = new Trie<Tuple<Operator<T>, int>>();

            for (int i = 0; i < operations.Length; i++)
            {
                foreach (KeyValuePair<string, Operator<T>> kvp in operations[i])
                {
                    result.Add(kvp.Key, new Tuple<Operator<T>, int>(kvp.Value, i));
                }
            }

            return result;
        }
    }
}
