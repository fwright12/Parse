using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.BiEnumerable;

namespace System.BiEnumerable
{
    public interface IBiEnumerable<T> : IEnumerable<T>
    {
        new IBiEnumerator<T> GetEnumerator();
    }

    public interface IBiEnumerator<T> : IEnumerator<T>
    {
        bool Move(int n);
        bool MovePrev();
    }
}

namespace Parse
{
    public interface IEditEnumerable<T> : IBiEnumerable<T>
    {
        new IEditEnumerator<T> GetEnumerator();
    }

    public interface IEditEnumerator<T> : IBiEnumerator<T>
    {
        void Add(int n, T t);
        bool Remove(int n);

        IEditEnumerator<T> Copy();
    }

    public interface IOrdered<T> : ICollection<T>, IEditEnumerable<T> { }
}
