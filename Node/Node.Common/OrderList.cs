using System.Collections;

namespace Node.Common;

public class OrderList<T> : IEnumerable<T>
{
    public int Count => Items.Count + ItemsLast.Count;

    readonly List<T> Items = new();
    readonly List<T> ItemsLast = new();

    public void AddFirst(T item) => Items.Insert(0, item);
    public void Add(T item) => Items.Add(item);
    public void AddLast(T item) => ItemsLast.Add(item);

    public IEnumerator<T> GetEnumerator() => Items.Concat(ItemsLast).GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
