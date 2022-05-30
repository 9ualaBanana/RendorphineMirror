using System.Collections;

namespace ReepoBot.Models;

public class Subscriptions : ICollection<long>
{
    readonly string _fileName;
    readonly HashSet<long> _subscriptions;

    public Subscriptions(string fileName)
    {
        _fileName = fileName;
        if (!File.Exists(_fileName))
        {
            using var _ = File.Create(_fileName);
        }
        _subscriptions = Load(_fileName)!;
    }

    public int Count => _subscriptions.Count;

    public bool IsReadOnly => false;

    static HashSet<long>? Load(string fileName)
    {
        return File.ReadAllLines(fileName).Select(chatId => long.Parse(chatId)).ToHashSet();
    }

    public void Add(long item)
    {
        var count = _subscriptions.Count;
        _subscriptions.Add(item);
        if (_subscriptions.Count != count)
        {
            File.AppendAllText(_fileName, $"{item}\n");
        }
    }

    public void Clear()
    {
        _subscriptions.Clear();
        File.Delete(_fileName);
        using var _ = File.Create(_fileName);
    }

    public bool Contains(long item)
    {
        return _subscriptions.Contains(item);
    }

    public void CopyTo(long[] array, int arrayIndex)
    {
        _subscriptions.CopyTo(array, arrayIndex);
    }

    public IEnumerator<long> GetEnumerator()
    {
        return _subscriptions.GetEnumerator();
    }

    public bool Remove(long item)
    {
        var result = _subscriptions.Remove(item);
        if (result)
        {
            File.WriteAllLines(_fileName, _subscriptions.Cast<string>());
        }
        return result;
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}
