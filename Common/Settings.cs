using System.Collections.Immutable;
using System.Data.Common;
using System.Data.SQLite;
using System.Diagnostics.CodeAnalysis;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Common
{
    public static class Settings
    {
        public static event Action? AnyChanged;
        public static readonly ImmutableArray<IDatabaseBindable> Bindables;

        public static string ServerUrl { get => BServerUrl.Value; set => BServerUrl.Value = value; }
        public static ushort LocalListenPort { get => BLocalListenPort.Value; set => BLocalListenPort.Value = value; }
        public static ushort UPnpPort { get => BUPnpPort.Value; set => BUPnpPort.Value = value; }
        public static ushort DhtPort { get => BDhtPort.Value; set => BDhtPort.Value = value; }
        public static ushort TorrentPort { get => BTorrentPort.Value; set => BTorrentPort.Value = value; }
        public static string? SessionId { get => BSessionId.Value; set => BSessionId.Value = value; }
        public static string? Email { get => BEmail.Value; set => BEmail.Value = value; }
        public static string? Guid { get => BGuid.Value; set => BGuid.Value = value; }
        public static string NodeName { get => BNodeName.Value!; set => BNodeName.Value = value!; }
        public static string? Language { get => BLanguage.Value; set => BLanguage.Value = value; }
        public static bool ShortcutsCreated { get => BShortcutsCreated.Value; set => BShortcutsCreated.Value = value; }

        public static readonly DatabaseBindable<string> BServerUrl;
        public static readonly DatabaseBindable<ushort> BLocalListenPort, BUPnpPort, BDhtPort, BTorrentPort;
        public static readonly DatabaseBindable<string?> BSessionId, BEmail, BGuid, BNodeName, BLanguage;
        public static readonly DatabaseBindable<bool> BShortcutsCreated;

        static readonly SQLiteConnection Connection;
        const string ConfigTable = "config";

        static Settings()
        {
            var dbfile = Path.Combine(Init.ConfigDirectory, "config.db");
            Connection = new SQLiteConnection("Data Source=" + dbfile + ";Version=3;cache=shared");
            if (!File.Exists(dbfile)) SQLiteConnection.CreateFile(dbfile);
            Connection.Open();
            CreateDBTable();


            BServerUrl = new(nameof(ServerUrl), "https://t.microstock.plus:8443");
            BLocalListenPort = new(nameof(LocalListenPort), 5123);
            BUPnpPort = new(nameof(UPnpPort), 5124);
            BDhtPort = new(nameof(DhtPort), 6223);
            BTorrentPort = new(nameof(TorrentPort), 6224);
            BSessionId = new(nameof(SessionId), null) { Hidden = true };
            BEmail = new(nameof(Email), null);
            BGuid = new(nameof(Guid), null);
            BNodeName = new(nameof(NodeName), null);
            BLanguage = new(nameof(Language), null);
            BShortcutsCreated = new(nameof(ShortcutsCreated), false);


            Bindables = typeof(Settings).GetFields(System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public)
                .Where(x => x.FieldType.IsAssignableTo(typeof(IDatabaseBindable)))
                .Select(x => (IDatabaseBindable) x.GetValue(null)!)
                .ToImmutableArray();
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
                Log.Fatal(ex.ToString());
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


        public interface IDatabaseBindable
        {
            bool Hidden { get; init; }
            string Name { get; }

            void Reload();

            JToken ToJson();
            void SetFromJson(string json);
        }
        public class DatabaseBindable<T> : Bindable<T>, IDatabaseBindable
        {
            public bool Hidden { get; init; }
            public string Name { get; }

            public DatabaseBindable(string name, T defaultValue = default!) : base(defaultValue)
            {
                Name = name;

                Reload();
                Changed += (oldv, newv) =>
                {
                    Set(name, newv);
                    AnyChanged?.Invoke();
                };
            }

            public void Reload() => Value = Get(Name, _Value)!;

            public JToken ToJson() => JToken.FromObject(Value!);
            public void SetFromJson(string json) => Value = JsonConvert.DeserializeObject<T>(json)!;


            [return: NotNullIfNotNull("defaultValue")]
            static T? Get(string path, T defaultValue)
            {
                using var query = ExecuteQuery(@$"select value from {ConfigTable} where key=@key;", new SQLiteParameter("key", path));
                if (!query.Read()) return defaultValue;

                var str = query.GetString(0);
                return JsonConvert.DeserializeObject<T>(str);
            }
            static void Set(string path, T value) =>
                ExecuteNonQuery(@$"insert into {ConfigTable}(key,value) values (@key, @value) on conflict(key) do update set value=@value;",
                    new SQLiteParameter("key", path),
                    new SQLiteParameter("value", JsonConvert.SerializeObject(value))
                );
        }
    }
}