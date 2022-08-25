﻿using System.Collections;
using System.Data.Common;
using System.Data.SQLite;
using System.Diagnostics.CodeAnalysis;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Common
{
    public readonly struct AuthInfo
    {
        public readonly string SessionId, Email, Guid;
        public readonly bool Slave;

        public AuthInfo(string sessionId, string email, string guid, bool slave = false)
        {
            SessionId = sessionId;
            Email = email;
            Guid = guid;
            Slave = slave;
        }
    }
    public static class Settings
    {
        readonly static Logger _logger = LogManager.GetCurrentClassLogger();

        public static event Action? AnyChanged;
        public static IReadOnlyList<IDatabaseBindable> Bindables => _Bindables;
        static readonly List<IDatabaseBindable> _Bindables = new();
        public static readonly string DbPath;

        public static string? SessionId => AuthInfo?.SessionId;
        public static string? Email => AuthInfo?.Email;
        public static string? Guid => AuthInfo?.Guid;
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
            BLocalListenPort = new(nameof(LocalListenPort), 5123);
            BUPnpPort = new(nameof(UPnpPort), 5124);
            BUPnpServerPort = new(nameof(UPnpServerPort), 5125);
            BDhtPort = new(nameof(DhtPort), 6223);
            BTorrentPort = new(nameof(TorrentPort), 6224);
            BAuthInfo = new(nameof(AuthInfo), default);
            BNodeName = new(nameof(NodeName), null);
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

                Bindable.LoadFromJson(JToken.Parse(query.GetString(0)), LocalApi.JsonSerializerWithType);
            }
            public void Save()
            {
                ExecuteNonQuery(@$"insert into {ConfigTable}(key,value) values (@key, @value) on conflict(key) do update set value=@value;",
                    new SQLiteParameter("key", Name),
                    new SQLiteParameter("value", JsonConvert.SerializeObject(Value, LocalApi.JsonSettingsWithType))
                );
            }
        }

        public class DatabaseValue<T> : DatabaseValueBase<T, Bindable<T>>, IDatabaseValueBindable<T>
        {
            public new T Value { get => Bindable.Value; set => Bindable.Value = value; }

            public DatabaseValue(string name, T defaultValue) : base(name, new(defaultValue)) { }
        }
        public class DatabaseValueList<T> : DatabaseValueBase<IReadOnlyList<T>, BindableList<T>>, IEnumerable<T>
        {
            public int Count => Bindable.Count;

            public DatabaseValueList(string name, IEnumerable<T>? values = null) : base(name, new(values)) { }

            public IEnumerator<T> GetEnumerator() => Bindable.GetEnumerator();
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }
        public class DatabaseValueDictionary<TKey, TValue> : DatabaseValueBase<IReadOnlyDictionary<TKey, TValue>, BindableDictionary<TKey, TValue>>, IEnumerable<KeyValuePair<TKey, TValue>> where TKey : notnull
        {
            public int Count => Bindable.Count;

            public DatabaseValueDictionary(string name, IEnumerable<KeyValuePair<TKey, TValue>>? values = null) : base(name, new(values)) { }

            public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() => Bindable.GetEnumerator();
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        public class DatabaseValueSplitList<T> : IDatabaseBindable, IReadOnlyList<T>
        {
            public int Count => Items.Count;
            readonly List<T> Items = new();

            readonly string TableName;

            public DatabaseValueSplitList(string table)
            {
                TableName = table;
                Reload();
            }

            public T this[int index] => Items[index];
            public void Add(T value)
            {
                Items.Add(value);

                ExecuteNonQuery(@$"insert into {TableName}(value) values (@value)",
                    new SQLiteParameter("value", JsonConvert.SerializeObject(value, LocalApi.JsonSettingsWithType))
                );
            }

            public void Reload()
            {
                ExecuteNonQuery($"create table if not exists {TableName} (value text null);");
                var reader = ExecuteQuery($"select * from {TableName} order by rowid");

                while (reader.Read())
                {
                    var valuejson = reader.GetString(reader.GetOrdinal("value"));
                    Items.Add(JToken.Parse(valuejson).ToObject<T>(LocalApi.JsonSerializerWithType)!);
                }
            }


            public IEnumerator<T> GetEnumerator() => Items.GetEnumerator();
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }
        public class DatabaseValueSplitDictionary<TKey, TValue> : IDatabaseBindable, IReadOnlyDictionary<TKey, TValue> where TKey : notnull
        {
            public IEnumerable<TKey> Keys => Items.Keys;
            public IEnumerable<TValue> Values => Items.Values;

            public int Count => Items.Count;
            readonly Dictionary<TKey, TValue> Items = new();

            readonly string TableName;

            public DatabaseValueSplitDictionary(string table)
            {
                TableName = table;
                Reload();
            }

            public TValue this[TKey key] => Items[key];
            public bool ContainsKey(TKey key) => Items.ContainsKey(key);
            public bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value) => Items.TryGetValue(key, out value);

            public void Add(TKey key, TValue value)
            {
                Items.Add(key, value);

                ExecuteNonQuery(@$"insert into {TableName}(key, value) values (@key, @value)",
                    new SQLiteParameter("key", JsonConvert.SerializeObject(key, LocalApi.JsonSettingsWithType)),
                    new SQLiteParameter("value", JsonConvert.SerializeObject(value, LocalApi.JsonSettingsWithType))
                );
            }

            public void Reload()
            {
                ExecuteNonQuery($"create table if not exists {TableName} (key text primary key unique, value text null);");
                var reader = ExecuteQuery($"select * from {TableName}");

                while (reader.Read())
                {
                    var key = JToken.Parse(reader.GetString(reader.GetOrdinal("key"))).ToObject<TKey>(LocalApi.JsonSerializerWithType)!;
                    var value = JToken.Parse(reader.GetString(reader.GetOrdinal("value"))).ToObject<TValue>(LocalApi.JsonSerializerWithType)!;
                    Items.Add(key, value);
                }
            }


            public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() => Items.GetEnumerator();
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }
    }
}