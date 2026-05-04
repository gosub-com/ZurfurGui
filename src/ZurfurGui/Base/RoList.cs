using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZurfurGui.Base;

/// <summary>
/// Allow quick read-only access to a list.  No garbage created when using foreach.
/// </summary>
public struct RoList<T> : IEnumerable<T>, IReadOnlyList<T>
{
    List<T> _list;
    public RoList() => _list = new List<T>();
    public RoList(List<T> list) => _list = list;
    public int Count => _list.Count;
    public T this[int index] => _list[index];
    public int FindIndex(Predicate<T> f) => _list.FindIndex(f);

    public List<T>.Enumerator GetEnumerator() => _list.GetEnumerator();
    IEnumerator<T> IEnumerable<T>.GetEnumerator() => _list.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)_list).GetEnumerator();
}
