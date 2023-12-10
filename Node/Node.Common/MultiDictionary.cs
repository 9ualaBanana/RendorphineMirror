namespace Node.Common;

public class MultiDictionary<TKey, TValue> : Dictionary<TKey, TValue> where TKey : notnull
{
    public MultiDictionary() { }
    public MultiDictionary(IDictionary<TKey, TValue> dictionary) : base(dictionary) { }
    public MultiDictionary(IEnumerable<KeyValuePair<TKey, TValue>> collection) : base(collection) { }
    public MultiDictionary(IEqualityComparer<TKey>? comparer) : base(comparer) { }
    public MultiDictionary(int capacity) : base(capacity) { }
    public MultiDictionary(IDictionary<TKey, TValue> dictionary, IEqualityComparer<TKey>? comparer) : base(dictionary, comparer) { }
    public MultiDictionary(IEnumerable<KeyValuePair<TKey, TValue>> collection, IEqualityComparer<TKey>? comparer) : base(collection, comparer) { }
    public MultiDictionary(int capacity, IEqualityComparer<TKey>? comparer) : base(capacity, comparer) { }

    public void Add(IEnumerable<KeyValuePair<TKey, TValue>> values)
    {
        foreach (var (key, value) in values)
            Add(key, value);
    }
}
