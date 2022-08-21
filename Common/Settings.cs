using System.Collections;
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
        const string ConfigTable = "config";

        static Settings()
        {
            DbPath = Path.Combine(Init.ConfigDirectory, "config.db");
            Connection = new SQLiteConnection("Data Source=" + DbPath + ";Version=3;cache=shared");
            if (!File.Exists(DbPath)) SQLiteConnection.CreateFile(DbPath);
            Connection.Open();
            CreateDBTable();


            BServerUrl = new(nameof(ServerUrl), "https://t.microstock.plus:8443");
            BLocalListenPort = new(nameof(LocalListenPort), 5123);
            BUPnpPort = new(nameof(UPnpPort), 5124);
            BUPnpServerPort = new(nameof(UPnpServerPort), 5125);
            BDhtPort = new(nameof(DhtPort), 6223);
            BTorrentPort = new(nameof(TorrentPort), 6224);
            BAuthInfo = new(nameof(AuthInfo), default);
            BNodeName = new(nameof(NodeName), null);
        }


        static void CreateDBTable()
        {
            try
            {
                ExecuteNonQuery($"create table if not exists {ConfigTable} (key text primary key unique, value text null);");
                ExecuteNonQuery("PRAGMA cache=shared;");
            }
            catch (SQLiteException ex)
            {
                _logger.Fatal(ex.ToString());
                throw;
            }
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

        [return: NotNullIfNotNull("defaultValue")]
        static string Load(string path, string defaultValue)
        {
            using var query = ExecuteQuery(@$"select value from {ConfigTable} where key=@key;", new SQLiteParameter("key", path));

            if (!query.Read()) return defaultValue;
            return query.GetString(0);
        }

        [return: NotNullIfNotNull("defaultValue")]
        static T? Load<T>(string path, T? defaultValue)
        {
            using var query = ExecuteQuery(@$"select value from {ConfigTable} where key=@key;", new SQLiteParameter("key", path));
            if (!query.Read()) return defaultValue;

            var str = query.GetString(0);
            return JsonConvert.DeserializeObject<T>(str, LocalApi.JsonSettingsWithType);
        }
        static void Save<T>(string path, T value)
        {
            ExecuteNonQuery(@$"insert into {ConfigTable}(key,value) values (@key, @value) on conflict(key) do update set value=@value;",
                new SQLiteParameter("key", path),
                new SQLiteParameter("value", JsonConvert.SerializeObject(value, LocalApi.JsonSettingsWithType))
            );
        }


        public interface IDatabaseBindable : IBindable
        {
            string Name { get; }

            void Save();
            void Reload();
        }
        public interface IDatabaseBindable<T> : IDatabaseBindable, IReadOnlyBindable<T> { }

        public abstract class DatabaseValueBase<T, TBindable> : IDatabaseBindable<T> where TBindable : IReadOnlyBindable<T>
        {
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
                    Settings.Save(name, Value);
                    AnyChanged?.Invoke();
                };

                _Bindables.Add(this);
            }

            public void Reload()
            {
                if (Load(Name) is { } jstr)
                    Bindable.LoadFromJson(JToken.Parse(jstr), LocalApi.JsonSerializerWithType);
            }


            static string? Load(string path)
            {
                using var query = ExecuteQuery(@$"select value from {ConfigTable} where key=@key;", new SQLiteParameter("key", path));

                if (!query.Read()) return null;
                return query.GetString(0);
            }
            public void Save() => Settings.Save(Name, Value);

            JToken IBindable.AsJson(JsonSerializer? serializer) => Bindable.AsJson(serializer);
            void IBindable.LoadFromJson(JToken json, JsonSerializer? serializer) => Bindable.LoadFromJson(json, serializer);
        }

        public class DatabaseValue<T> : DatabaseValueBase<T, Bindable<T>>, IBindable<T>
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
        public class DatabaseValueDictionary<TKey, TValue> : DatabaseValueBase<IReadOnlyDictionary<TKey, TValue>, BindableDictionary<TKey, TValue>>, IEnumerable<KeyValuePair<TKey,TValue>> where TKey : notnull
        {
            public int Count => Bindable.Count;

            public DatabaseValueDictionary(string name, IEnumerable<KeyValuePair<TKey, TValue>>? values = null) : base(name, new(values)) { }

            public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() => Bindable.GetEnumerator();
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }
    }
}