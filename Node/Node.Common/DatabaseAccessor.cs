using System.Data.SQLite;

namespace Node.Common;

public class DatabaseAccessor<TKey, TValue>
    where TKey : notnull
{
    readonly object LockObject = new();
    public JsonSerializer Serializer { get; init; } = JsonSettings.TypedS;
    public Database Database { get; }
    public string TableName { get; }

    public DatabaseAccessor(Database database, string tableName, string tableKeyType = "text")
    {
        Database = database;
        TableName = tableName;

        Database.ExecuteNonQuery($"create table if not exists {TableName} (key {tableKeyType} primary key unique not null, value text null);");
    }

    public SQLiteParameter Parameter<T>(string name, T value) =>
        new SQLiteParameter(name,
            value is string str ? $"\"{str}\""
            : value is null ? JValue.CreateNull().ToString()
            : JToken.FromObject(value, Serializer).ToString()
        );

    public void Add(TKey key, TValue value)
    {
        lock (LockObject)
        {
            Database.ExecuteNonQuery(@$"insert into {TableName}(key, value) values (@key, @value)",
                Parameter("key", key),
                Parameter("value", value)
            );
        }
    }
    public void AddRange(IEnumerable<KeyValuePair<TKey, TValue>> values)
    {
        lock (LockObject)
            foreach (var (key, value) in values)
                Add(key, value);
    }

    public IEnumerable<KeyValuePair<TKey, TValue>> GetWhere(string where, IEnumerable<SQLiteParameter> parameters)
    {
        lock (LockObject)
        {
            using var query = Database.ExecuteQuery(@$"select key, value from {TableName} where {where}", parameters);

            while (query.Read())
            {
                var key = query.GetValue(0);
                var strkey = key is string str ? str : key.ToString()!;

                var value = query.GetString(1);

                JToken jt;
                try { jt = JToken.Parse(value); }
                catch
                {
                    try { jt = JValue.FromObject(value); }
                    catch { jt = value; }
                }

                yield return KeyValuePair.Create(
                    JToken.Parse(strkey).ToObject<TKey>().ThrowIfNull(),
                    jt.ToObject<TValue>(Serializer).ThrowIfNull()
                );
            }
        }
    }

    public void Remove(TKey key)
    {
        lock (LockObject)
        {
            Database.ExecuteNonQuery(@$"delete from {TableName} where key=@key",
                Parameter("key", key)
            );
        }
    }

    public void Clear()
    {
        lock (LockObject)
            Database.ExecuteNonQuery(@$"delete from {TableName}");
    }
}
