using System.Data.Common;
using System.Data.SQLite;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

namespace Common
{
    public static class Settings
    {
        public static string ServerUrl { get => BServerUrl.Value; set => BServerUrl.Value = value; }
        public static ushort LocalListenPort { get => BLocalListenPort.Value; set => BLocalListenPort.Value = value; }
        public static ushort UPnpPort { get => BUPnpPort.Value; set => BUPnpPort.Value = value; }
        public static ushort DhtPort { get => BDhtPort.Value; set => BDhtPort.Value = value; }
        public static ushort TorrentPort { get => BTorrentPort.Value; set => BTorrentPort.Value = value; }
        public static string? SessionId { get => BSessionId.Value; set => BSessionId.Value = value; }
        public static string? UserId { get => BUserId.Value; set => BUserId.Value = value; }
        public static string? Username { get => BUsername.Value; set => BUsername.Value = value; }
        public static string? Language { get => BLanguage.Value; set => BLanguage.Value = value; }

        public static readonly DatabaseBindable<string> BServerUrl;
        public static readonly DatabaseBindable<ushort> BLocalListenPort, BUPnpPort, BDhtPort, BTorrentPort;
        public static readonly DatabaseBindable<string?> BSessionId, BUsername, BUserId, BLanguage;

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
            BSessionId = new(nameof(SessionId), null);
            BUsername = new(nameof(Username), null);
            BUserId = new(nameof(UserId), null);
            BLanguage = new(nameof(Language), null);
        }


        [return: NotNullIfNotNull("defaultValue")]
        static T? Get<T>(string path, T defaultValue)
        {
            using var query = ExecuteQuery(@$"select value from {ConfigTable} where key=@key;", new SQLiteParameter("key", path));
            if (!query.Read()) return defaultValue;

            var str = query.GetString(0);
            return JsonSerializer.Deserialize<T>(str);
        }
        static void Set<T>(string path, T value) =>
            ExecuteNonQuery(@$"insert into {ConfigTable}(key,value) values (@key, @value) on conflict(key) do update set value=@value;",
                new SQLiteParameter("key", path),
                new SQLiteParameter("value", JsonSerializer.Serialize(value))
            );

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


        public class DatabaseBindable<T> : Bindable<T>
        {
            readonly string Name;

            public DatabaseBindable(string name, T defaultValue = default!) : base(defaultValue)
            {
                Name = name;

                Reload();
                Changed += (oldv, newv) =>
                    ExecuteNonQuery(@$"insert into {ConfigTable}(key,value) values (@key, @value) on conflict(key) do update set value=@value;",
                        new SQLiteParameter("key", name),
                        new SQLiteParameter("value", JsonSerializer.Serialize(newv))
                    );
            }

            public void Reload() => _Value = Get(Name, _Value)!;
        }
    }
}