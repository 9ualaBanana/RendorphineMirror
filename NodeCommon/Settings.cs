using System.Collections;
using System.Data.Common;
using System.Data.SQLite;
using System.Diagnostics.CodeAnalysis;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace NodeCommon
{
    public readonly struct AuthInfo
    {
        public readonly string SessionId, Guid, UserId;
        public readonly string? Email;
        public readonly bool Slave;

        public AuthInfo(string sessionId, string? email, string guid, string userid = null!, bool slave = false)
        {
            SessionId = sessionId;
            Email = email;
            Guid = guid;
            Slave = slave;
            UserId = userid;
        }
    }
    public static class Settings
    {
        readonly static Logger _logger = LogManager.GetCurrentClassLogger();

        public static event Action? AnyChanged;
        public static IReadOnlyList<IDatabaseBindable> Bindables => _Bindables;
        static readonly List<IDatabaseBindable> _Bindables = new();
        public static readonly string DbPath;

        public static string SessionId => AuthInfo?.SessionId!;
        public static string? Email => AuthInfo?.Email;
        public static string Guid => AuthInfo?.Guid!;
        public static string UserId => AuthInfo?.UserId!;
        public static bool? IsSlave => AuthInfo?.Slave;
        public static AuthInfo? AuthInfo { get => BAuthInfo.Value; set => BAuthInfo.Value = value; }

        public const string RegistryUrl = "https://t.microstock.plus:7897";
        public static string ServerUrl { get => BServerUrl.Value; set => BServerUrl.Value = value; }
        public static ushort LocalListenPort { get => BLocalListenPort.Value; set => BLocalListenPort.Value = value; }
        public static ushort UPnpPort { get => BUPnpPort.Value; set => BUPnpPort.Value = value; }
        public static ushort UPnpServerPort { get => BUPnpServerPort.Value; set => BUPnpServerPort.Value = value; }
        public static ushort DhtPort { get => BDhtPort.Value; set => BDhtPort.Value = value; }
        public static ushort TorrentPort { get => BTorrentPort.Value; set => BTorrentPort.Value = value; }
        public static string NodeName { get => BNodeName.Value!; set => BNodeName.Value = value!; }

        public static readonly DatabaseValue<string> BServerUrl;
        public static readonly DatabaseValue<ushort> BLocalListenPort, BUPnpPort, BUPnpServerPort, BDhtPort, BTorrentPort;
        public static readonly DatabaseValue<string?> BNodeName;
        public static readonly DatabaseValue<AuthInfo?> BAuthInfo;

        static readonly SQLiteConnection Connection;

        static Settings()
        {
            DbPath = Path.Combine(Init.ConfigDirectory, "config.db");
            Connection = new SQLiteConnection("Data Source=" + DbPath + ";Version=3;cache=shared");
            if (!File.Exists(DbPath)) SQLiteConnection.CreateFile(DbPath);
            Connection.Open();
            OperationResult.WrapException(() => ExecuteNonQuery("PRAGMA cache=shared;")).LogIfError();


            BServerUrl = new(nameof(ServerUrl), "https://t.microstock.plus:8443");
            BLocalListenPort = new(nameof(LocalListenPort), randomized(5123));
            BUPnpPort = new(nameof(UPnpPort), randomized(5223));
            BUPnpServerPort = new(nameof(UPnpServerPort), randomized(5323));
            BDhtPort = new(nameof(DhtPort), randomized(6223));
            BTorrentPort = new(nameof(TorrentPort), randomized(6323));
            BAuthInfo = new(nameof(AuthInfo), default);
            BNodeName = new(nameof(NodeName), null);

            foreach (var bindable in new[] { BLocalListenPort, BUPnpPort, BUPnpServerPort, BDhtPort, BTorrentPort })
                bindable.Save();


            static ushort randomized(ushort port) => (ushort) (port + Random.Shared.Next(80));
        }

        static int ExecuteNonQuery(string command)
        {
            using var cmd = new SQLiteCommand(command, Connection);
            return cmd.ExecuteNonQuery();
        }
        static int ExecuteNonQuery(string command, params DbParameter[] parameters) => ExecuteNonQuery(command, parameters.AsEnumerable());
        static int ExecuteNonQuery(string command, IEnumerable<DbParameter> parameters)
        {
            using var cmd = new SQLiteCommand(command, Connection);

            foreach (var parameter in parameters)
                cmd.Parameters.Add(parameter);

            return cmd.ExecuteNonQuery();
        }

        static DbDataReader ExecuteQuery(string command)
        {
            using var cmd = new SQLiteCommand(command, Connection);
            return cmd.ExecuteReader();
        }
        static DbDataReader ExecuteQuery(string command, params DbParameter[] parameters) => ExecuteQuery(command, parameters.AsEnumerable());
        static DbDataReader ExecuteQuery(string command, IEnumerable<DbParameter> parameters)
        {
            using var cmd = new SQLiteCommand(command, Connection);

            foreach (var parameter in parameters)
                cmd.Parameters.Add(parameter);

            return cmd.ExecuteReader();
        }

        public static void Reload()
        {
            foreach (var bindable in Bindables)
                bindable.Reload();
        }


        public interface IDatabaseBindable
        {
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
            public readonly TBindable Bindable;


            public DatabaseValueBase(string name, TBindable bindable)
            {
                Name = name;
                Bindable = bindable;

                Reload();
                Bindable.Changed += () =>
                {
                    Save();
                    AnyChanged?.Invoke();
                };

                _Bindables.Add(this);
            }

            public void Reload()
            {
                ExecuteNonQuery($"create table if not exists {ConfigTable} (key text primary key unique, value text null);");

                using var query = ExecuteQuery(@$"select value from {ConfigTable} where key=@key;", new SQLiteParameter("key", Name));
                if (!query.Read()) return;

                Bindable.LoadFromJson(JToken.Parse(query.GetString(0)), JsonSettings.TypedS);
            }
            public void Save()
            {
                ExecuteNonQuery(@$"insert into {ConfigTable}(key,value) values (@key, @value) on conflict(key) do update set value=@value;",
                    new SQLiteParameter("key", Name),
                    new SQLiteParameter("value", JsonConvert.SerializeObject(Value, JsonSettings.Typed))
                );
            }

            public void Delete()
            {
                ExecuteNonQuery(@$"delete from {ConfigTable} where key=@key;",
                    new SQLiteParameter("key", Name)
                );
            }
        }

        public class DatabaseValue<T> : DatabaseValueBase<T, Bindable<T>>, IDatabaseValueBindable<T>
        {
            public new T Value { get => Bindable.Value; set => Bindable.Value = value; }

            public DatabaseValue(string name, T defaultValue) : base(name, new(defaultValue)) { }
        }

        public class DatabaseValueKeyDictionary<TKey, TValue> : DatabaseValueDictionary<TKey, KeyValuePair<TKey, TValue>> where TKey : notnull
        {
            public DatabaseValueKeyDictionary(string table, IEqualityComparer<TKey>? comparer = null) : base(table, k => k.Key, comparer) { }

            public void Add(TKey key, TValue value) => base.Add(KeyValuePair.Create(key, value));
        }

        public class DatabaseValueDictionary<TKey, TValue> : IDatabaseBindable, IReadOnlyDictionary<TKey, TValue> where TKey : notnull
        {
            public IEnumerable<TKey> Keys => Items.Keys;
            public IEnumerable<TValue> Values => Items.Values;
            public int Count => Items.Count;

            public BindableBase<IReadOnlyList<TValue>> Bindable => ItemsList;
            readonly BindableList<TValue> ItemsList = new();
            readonly Dictionary<TKey, TValue> Items;

            readonly Func<TValue, TKey> KeyFunc;
            readonly string TableName;

            public DatabaseValueDictionary(string table, Func<TValue, TKey> keyFunc, IEqualityComparer<TKey>? comparer = null)
            {
                Items = new(comparer);

                TableName = table;
                KeyFunc = keyFunc;

                Reload();
            }

            static SQLiteParameter Parameter<T>(string name, T value) => new SQLiteParameter(name, JsonConvert.SerializeObject(value, JsonSettings.Typed));

            public TValue this[TKey key] => Items[key];
            public void Add(TValue value)
            {
                var key = KeyFunc(value);
                ItemsList.Add(value);
                Items.Add(key, value);

                ExecuteNonQuery(@$"insert into {TableName}(key, value) values (@key, @value)",
                    Parameter("key", key),
                    Parameter("value", value)
                );
            }
            public void AddRange(IEnumerable<TValue> values)
            {
                foreach (var value in values)
                    Add(value);
            }

            public void Remove(TValue value) => Remove(KeyFunc(value));
            public void Remove(TKey key)
            {
                if (Items.TryGetValue(key, out var value))
                    ItemsList.Remove(value);
                Items.Remove(key);

                ExecuteNonQuery(@$"delete from {TableName} where key=@key", Parameter("key", key));
            }

            public void Clear()
            {
                ItemsList.Clear();
                Items.Clear();
                ExecuteNonQuery(@$"delete from {TableName}");
            }

            public void Reload()
            {
                Items.Clear();
                ItemsList.Clear();

                ExecuteNonQuery($"create table if not exists {TableName} (key text primary key unique not null, value text null);");
                var reader = ExecuteQuery($"select * from {TableName} order by rowid");

                while (reader.Read())
                {
                    var valuejson = reader.GetString(reader.GetOrdinal("value"));
                    var item = JToken.Parse(valuejson).ToObject<TValue>(JsonSettings.TypedS)!;

                    Items.Add(KeyFunc(item), item);
                    ItemsList.Add(item);
                }
            }
            public void Save(TValue value)
            {
                ExecuteNonQuery($"insert into {TableName}(key,value) values (@key, @value) on conflict(key) do update set value=@value;",
                    Parameter("key", KeyFunc(value)),
                    Parameter("value", value)
                );
            }

            public bool ContainsKey(TKey key) => Items.ContainsKey(key);
            public bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value) => Items.TryGetValue(key, out value);


            public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() => Items.GetEnumerator();
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }
    }
}