using System.Collections;
using System.Data.SQLite;

namespace Node.Common;

public interface IDatabaseBindable
{
    IBindable Bindable { get; }

    void Reload();
}
public interface IDatabaseBindable<T> : IDatabaseBindable { }
public interface IDatabaseValueBindable<T> : IDatabaseBindable<T>
{
    T Value { get; set; }
}

public abstract class DatabaseValueBase<T, TBindable> : IDatabaseBindable<T> where TBindable : IReadOnlyBindable<T>
{
    protected const string ConfigTable = "config";

    public event Action? Changed { add => Bindable.Changed += value; remove => Bindable.Changed -= value; }
    public T Value => Bindable.Value;

    public string Name { get; }
    IBindable IDatabaseBindable.Bindable => Bindable;
    public readonly TBindable Bindable;
    readonly Database Database;


    public DatabaseValueBase(Database database, string name, TBindable bindable)
    {
        Database = database;

        Name = name;
        Bindable = bindable;

        Reload();
        Bindable.Changed += Save;
    }

    public void Reload()
    {
        Database.ExecuteNonQuery($"create table if not exists {ConfigTable} (key text primary key unique, value text null);");

        using var query = Database.ExecuteQuery(@$"select value from {ConfigTable} where key=@key;", new SQLiteParameter("key", Name));
        if (!query.Read()) return;

        JToken jt;
        try { jt = JToken.Parse(query.GetString(0)); }
        catch
        {
            try { jt = JValue.FromObject(query.GetString(0)); }
            catch { jt = query.GetString(0); }
        }

        Bindable.LoadFromJson(jt, null);
    }
    public void Save()
    {
        Database.ExecuteNonQuery(@$"insert into {ConfigTable}(key,value) values (@key, @value) on conflict(key) do update set value=@value;",
            new SQLiteParameter("key", Name),
            new SQLiteParameter("value", (Value is null ? JValue.CreateNull() : JToken.FromObject(Value)).ToString())
        );
    }

    public void Delete()
    {
        Database.ExecuteNonQuery(@$"delete from {ConfigTable} where key=@key;",
            new SQLiteParameter("key", Name)
        );
    }
}
public class DatabaseValue<T> : DatabaseValueBase<T, Bindable<T>>, IDatabaseValueBindable<T>
{
    public new T Value { get => Bindable.Value; set => Bindable.Value = value; }

    public DatabaseValue(Database database, string name, T defaultValue) : base(database, name, new(defaultValue)) { }
}

public class DatabaseValueKeyDictionary<TKey, TValue> : DatabaseValueDictionary<TKey, KeyValuePair<TKey, TValue>> where TKey : notnull
{
    public DatabaseValueKeyDictionary(Database database, string table, IEqualityComparer<TKey>? comparer = null) : base(database, table, k => k.Key, comparer) { }

    public void Add(TKey key, TValue value) => base.Add(KeyValuePair.Create(key, value));
}

public class DatabaseValueDictionary<TKey, TValue> : IDatabaseBindable, IReadOnlyDictionary<TKey, TValue> where TKey : notnull
{
    public IEnumerable<TKey> Keys => Items.Keys;
    public IEnumerable<TValue> Values => Items.Values;
    public int Count => Items.Count;
    readonly object LockObject = new();

    IBindable IDatabaseBindable.Bindable => Bindable;
    public BindableBase<IReadOnlyList<TValue>> Bindable => ItemsList;
    readonly BindableList<TValue> ItemsList = new();
    readonly Dictionary<TKey, TValue> Items;

    readonly DatabaseAccessor<TKey, TValue> Accessor;

    readonly Func<TValue, TKey> KeyFunc;
    readonly string TableName;
    public readonly Database Database;

    public DatabaseValueDictionary(Database database, string table, Func<TValue, TKey> keyFunc, IEqualityComparer<TKey>? comparer = null, JsonSerializer? serializer = null)
    {
        Database = database;

        if (serializer is not null) ItemsList.JsonSerializer = serializer;
        Accessor = new(database, table) { Serializer = ItemsList.JsonSerializer };
        Items = new(comparer);

        TableName = table;
        KeyFunc = keyFunc;

        Reload();
    }

    public TValue this[TKey key] => Items[key];
    public void Add(TValue value)
    {
        lock (LockObject)
        {
            var key = KeyFunc(value);
            ItemsList.Add(value);
            Items.Add(key, value);

            Accessor.Add(key, value);
        }
    }
    public void AddRange(IEnumerable<TValue> values)
    {
        lock (LockObject)
            foreach (var value in values)
                Add(value);
    }

    public void Remove(TValue value) => Remove(KeyFunc(value));
    public void Remove(TKey key)
    {
        lock (LockObject)
        {
            if (Items.TryGetValue(key, out var value))
                ItemsList.Remove(value);
            Items.Remove(key);

            Accessor.Remove(key);
        }
    }

    public void Clear()
    {
        lock (LockObject)
        {
            ItemsList.Clear();
            Items.Clear();
            Accessor.Clear();
        }
    }

    public void Reload()
    {
        lock (LockObject)
        {
            Items.Clear();
            ItemsList.Clear();

            Database.ExecuteNonQuery($"create table if not exists {TableName} (key text primary key unique not null, value text null);");
            var reader = Database.ExecuteQuery($"select * from {TableName} order by rowid");

            while (reader.Read())
            {
                var valuejson = reader.GetString(reader.GetOrdinal("value"));
                var item = JToken.Parse(valuejson).ToObject<TValue>(Bindable.JsonSerializer)!;

                Items.Add(KeyFunc(item), item);
                ItemsList.Add(item);
            }
        }
    }
    public void Save(TValue value)
    {
        Database.ExecuteNonQuery($"insert into {TableName}(key,value) values (@key, @value) on conflict(key) do update set value=@value;",
            Accessor.Parameter("key", KeyFunc(value)),
            Accessor.Parameter("value", value)
        );

        Bindable.TriggerValueChanged();
    }

    public bool ContainsKey(TKey key) => Items.ContainsKey(key);
    public bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value) => Items.TryGetValue(key, out value);


    public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() => Items.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
