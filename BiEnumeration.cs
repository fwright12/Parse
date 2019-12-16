using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace System.Collections
{
    public interface IBiEnumerable : IEnumerable
    {
        new IBiEnumerator GetEnumerator();
    }

    public interface IBiEnumerator : IEnumerator
    {
        bool Move(int n);
        bool MovePrev();
    }
}

namespace System.Collections.Generic
{
    public interface IBiEnumerable<out T> : IEnumerable<T>
    {
        new IBiEnumerator<T> GetEnumerator();
    }
    public interface IBiEnumerator<out T> : IBiEnumerator, IEnumerator<T> { }
}

namespace Parse//.Collections.Generic
{
    public interface IEditEnumerble : IBiEnumerable
    {
        new IEditEnumerator GetEnumerator();
    }

    public interface IEditEnumerable<T> : IBiEnumerable<T>
    {
        new IEditEnumerator<T> GetEnumerator();
    }

    public interface IEditEnumerator : IBiEnumerator
    {
        bool Remove(int n);
        IEditEnumerator Copy();
    }

    public interface IEditEnumerator<T> : IEditEnumerator, IBiEnumerator<T>
    {
        void Add(int n, T t);
        
        new IEditEnumerator<T> Copy();
    }

    public interface IOrdered<T> : ICollection<T>, IEditEnumerable<T> { }
}
