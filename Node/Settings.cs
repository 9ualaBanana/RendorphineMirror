using System.Data.Common;
using System.Data.SQLite;

namespace Node;

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

        typeof(Settings).GetFields(System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public)
            .Where(x => x.FieldType.IsAssignableTo(typeof(IBindable)))
            .Select(x => ((IBindable) x.GetValue(null)!, x.Name))
            .ToList()
            .ForEach(x => x.Item1.Changed += () => AnyChanged?.Invoke());

        foreach (var bindable in new[] { BLocalListenPort, BUPnpPort, BUPnpServerPort, BDhtPort, BTorrentPort })
            bindable.Save();


        static ushort randomized(ushort port) => (ushort) (port + Random.Shared.Next(80));
    }

    public static int ExecuteNonQuery(string command)
    {
        using var cmd = new SQLiteCommand(command, Connection);
        return cmd.ExecuteNonQuery();
    }
    public static int ExecuteNonQuery(string command, params DbParameter[] parameters) => ExecuteNonQuery(command, parameters.AsEnumerable());
    public static int ExecuteNonQuery(string command, IEnumerable<DbParameter> parameters)
    {
        using var cmd = new SQLiteCommand(command, Connection);

        foreach (var parameter in parameters)
            cmd.Parameters.Add(parameter);

        return cmd.ExecuteNonQuery();
    }

    public static DbDataReader ExecuteQuery(string command)
    {
        using var cmd = new SQLiteCommand(command, Connection);
        return cmd.ExecuteReader();
    }
    public static DbDataReader ExecuteQuery(string command, params DbParameter[] parameters) => ExecuteQuery(command, parameters.AsEnumerable());
    public static DbDataReader ExecuteQuery(string command, IEnumerable<DbParameter> parameters)
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
}