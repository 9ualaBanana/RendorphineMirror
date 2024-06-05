namespace Node;

public static class Settings
{
    public static string SessionId => AuthInfo?.SessionId!;
    public static string? Email => AuthInfo?.Email;
    public static string Guid => AuthInfo?.Guid!;
    public static string UserId => AuthInfo?.UserId!;
    public static bool? IsSlave => AuthInfo?.Slave;
    public static AuthInfo? AuthInfo { get => BAuthInfo.Value; set => BAuthInfo.Value = value; }

    public static string ServerUrl { get => BServerUrl.Value; set => BServerUrl.Value = value; }
    public static ushort LocalListenPort { get => BLocalListenPort.Value; set => BLocalListenPort.Value = value; }
    public static ushort UPnpPort { get => BUPnpPort.Value; set => BUPnpPort.Value = value; }
    public static ushort DhtPort { get => BDhtPort.Value; set => BDhtPort.Value = value; }
    public static ushort TorrentPort { get => BTorrentPort.Value; set => BTorrentPort.Value = value; }
    public static string NodeName { get => BNodeName.Value!; set => BNodeName.Value = value!; }

    public static DatabaseValue<string> BServerUrl => Instance.BServerUrl;
    public static DatabaseValue<ushort> BLocalListenPort => Instance.BLocalListenPort;
    public static DatabaseValue<ushort> BUPnpPort => Instance.BUPnpPort;
    public static DatabaseValue<ushort> BDhtPort => Instance.BDhtPort;
    public static DatabaseValue<ushort> BTorrentPort => Instance.BTorrentPort;
    public static DatabaseValue<string?> BNodeName => Instance.BNodeName;
    public static DatabaseValue<AuthInfo?> BAuthInfo => Instance.BAuthInfo;

    public static DatabaseValue<ImmutableArray<TaskAction>> DisabledTaskTypes => Instance.DisabledTaskTypes;
    public static DatabaseValue<bool> AcceptTasks => Instance.AcceptTasks;
    public static DatabaseValue<uint> TaskAutoDeletionDelayDays => Instance.TaskAutoDeletionDelayDays;
    public static DatabaseValue<SettingsInstance.BenchmarkInfo?> BenchmarkResult => Instance.BenchmarkResult;

    public static readonly SettingsInstance Instance = new(new DataDirs("renderfin"));
}
