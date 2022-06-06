using System.Data.Common;
using System.Data.SQLite;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

namespace Common
{
    public static class Settings
    {
        public static string ServerUrl { get => BServerUrl.Value; set => BServerUrl.Value = value; }
        public static ushort ListenPort { get => BListenPort.Value; set => BListenPort.Value = value; }
        public static ushort UPnpPort { get => BUPnpPort.Value; set => BUPnpPort.Value = value; }
        public static string? SessionId { get => BSessionId.Value; set => BSessionId.Value = value; }
        public static string? UserId { get => BUserId.Value; set => BUserId.Value = value; }
        public static string? Username { get => BUsername.Value; set => BUsername.Value = value; }
        public static string? Language { get => BLanguage.Value; set => BLanguage.Value = value; }
        public static LogLevel LogLevel { get => BLogLevel.Value; set => BLogLevel.Value = value; }

        public static readonly DatabaseBindable<string> BServerUrl;
        public static readonly DatabaseBindable<ushort> BListenPort, BUPnpPort;
        public static readonly DatabaseBindable<string?> BSessionId, BUsername, BUserId, BLanguage;
        public static readonly DatabaseBindable<LogLevel> BLogLevel;

        static readonly SQLiteConnection Connection;
        const string ConfigTable = "config";

        static Settings()
        {
            var dbfile = Path.Combine(Init.ConfigDirectory, "config.db");
            Connection = new SQLiteConnection("Data Source=" + dbfile + ";Version=3;");
            if (!File.Exists(dbfile)) SQLiteConnection.CreateFile(dbfile);
            Connection.Open();
            CreateDBTable();


            BServerUrl = new(nameof(ServerUrl), "https://t.microstock.plus:8443");
            BListenPort = new(nameof(ListenPort), 5123);
            BUPnpPort = new(nameof(UPnpPort), 5124);
            BSessionId = new(nameof(SessionId), null);
            BUsername = new(nameof(Username), null);
            BUserId = new(nameof(UserId), null);
            BLanguage = new(nameof(Language), null);
            BLogLevel = new(nameof(LogLevel), LogLevel.Basic);
        }


        [return: NotNullIfNotNull("defaultValue")]
        static T? Get<T>(string path, T defaultValue)
        {
            var query = ExecuteQuery(@$"select value from {ConfigTable} where key=@key;", new SQLiteParameter("key", path));
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
            try { ExecuteNonQuery(@$"create table if not exists {ConfigTable} (key text primary key unique, value text null);"); }
            catch (SQLiteException ex)
            {
                Logger.LogErr(ex);
                throw;
            }
        }

        static int ExecuteNonQuery(string command) => new SQLiteCommand(command, Connection).ExecuteNonQuery();
        static int ExecuteNonQuery(string command, params DbParameter[] parameters) => ExecuteNonQuery(command, parameters.AsEnumerable());
        static int ExecuteNonQuery(string command, IEnumerable<DbParameter> parameters)
        {
            var cmd = new SQLiteCommand(command, Connection);

            foreach (var parameter in parameters)
                cmd.Parameters.Add(parameter);

            return cmd.ExecuteNonQuery();
        }

        static DbDataReader ExecuteQuery(string command) => new SQLiteCommand(command, Connection).ExecuteReader();
        static DbDataReader ExecuteQuery(string command, params DbParameter[] parameters) => ExecuteQuery(command, parameters.AsEnumerable());
        static DbDataReader ExecuteQuery(string command, IEnumerable<DbParameter> parameters)
        {
            var cmd = new SQLiteCommand(command, Connection);

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

            public void Reload() => _Value = Get<T>(Name, _Value)!;
        }
    }
}