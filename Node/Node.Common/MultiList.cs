namespace Node.Common;

public interface IReadOnlyMultiList<T> : IReadOnlyList<T> where T : class { }
public interface IMultiList<T> : IList<T>, IReadOnlyMultiList<T> where T : class
{
    new void Add(T? item);
    void Add(params T?[]? items);
    void Add(IEnumerable<T?>? items);

    new void Insert(int index, T? item);
    void Insert(int index, params T?[]? items);
    void Insert(int index, IEnumerable<T?>? items);
}

/// <summary> A class for an easy creation of a list </summary>
public class MultiList<T> : IMultiList<T> where T : class
{
    bool ICollection<T>.IsReadOnly => false;

    public int Count => Items.Count;
    readonly List<T> Items = new();

    public T this[int index] { get => Items[index]; set => Items[index] = value; }

    public void Add(T? item)
    {
        if (item is not null)
            Items.Add(item);
    }
    public void Add(params T?[]? items) => Add(items?.AsEnumerable());
    public void Add(IEnumerable<T?>? items) => Items.AddRange(WhereNotNull(items));

    public void Insert(int index, T? item)
    {
        if (item is not null)
            Items.Insert(index, item);
    }
    public void Insert(int index, params T?[]? items) => Insert(index, items?.AsEnumerable());
    public void Insert(int index, IEnumerable<T?>? items) => Items.InsertRange(index, WhereNotNull(items));

    static IEnumerable<T> WhereNotNull(IEnumerable<T?>? items) => (items?.Where(x => x is not null) ?? Enumerable.Empty<T>())!;

    public bool Remove(T item) => Items.Remove(item);
    public void RemoveAt(int index) => Items.RemoveAt(index);
    public void Clear() => Items.Clear();

    public int IndexOf(T item) => Items.IndexOf(item);
    public bool Contains(T item) => Items.Contains(item);
    public void CopyTo(T[] array, int arrayIndex) => Items.CopyTo(array, arrayIndex);


    IEnumerator<T> IEnumerable<T>.GetEnumerator() => Items.GetEnumerator();
    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => Items.GetEnumerator();
}
