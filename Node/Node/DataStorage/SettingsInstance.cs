using Node.Profiling;

namespace Node.DataStorage;

public class SettingsInstance : INodeSettings
{
    public string SessionId => AuthInfo?.SessionId!;
    public string? Email => AuthInfo?.Email;
    public string Guid => AuthInfo?.Guid!;
    public string UserId => AuthInfo?.UserId!;
    public bool? IsSlave => AuthInfo?.Slave;
    public AuthInfo? AuthInfo { get => BAuthInfo.Value; set => BAuthInfo.Value = value; }

    public string ServerUrl { get => BServerUrl.Value; set => BServerUrl.Value = value; }
    public ushort LocalListenPort { get => BLocalListenPort.Value; set => BLocalListenPort.Value = value; }
    public ushort UPnpPort { get => BUPnpPort.Value; set => BUPnpPort.Value = value; }
    public ushort UPnpServerPort { get => BUPnpServerPort.Value; set => BUPnpServerPort.Value = value; }
    public ushort DhtPort { get => BDhtPort.Value; set => BDhtPort.Value = value; }
    public ushort TorrentPort { get => BTorrentPort.Value; set => BTorrentPort.Value = value; }
    public string NodeName { get => BNodeName.Value!; set => BNodeName.Value = value!; }

    public readonly DatabaseValue<string> BServerUrl;
    public readonly DatabaseValue<ushort> BLocalListenPort, BUPnpPort, BUPnpServerPort, BDhtPort, BTorrentPort;
    public readonly DatabaseValue<string?> BNodeName;
    public readonly DatabaseValue<AuthInfo?> BAuthInfo;

    public readonly DatabaseValue<ImmutableArray<TaskAction>> DisabledTaskTypes;
    public readonly DatabaseValue<bool> AcceptTasks;
    public readonly DatabaseValue<uint> TaskAutoDeletionDelayDays;
    public readonly DatabaseValue<BenchmarkInfo?> BenchmarkResult;
    public readonly DatabaseValue<string?> TaskProcessingDirectory;

    public SettingsInstance(DataDirs dirs)
    {
        static ushort randomized(ushort port) => (ushort) (port + Random.Shared.Next(80));

        var db = new Database(Path.Combine(dirs.Data, "config.db"));

        BServerUrl = new(db, nameof(ServerUrl), "https://t.microstock.plus:8443");
        BLocalListenPort = new(db, nameof(LocalListenPort), randomized(5123));
        BUPnpPort = new(db, nameof(UPnpPort), randomized(5223));
        BUPnpServerPort = new(db, nameof(UPnpServerPort), randomized(5323));
        BDhtPort = new(db, nameof(DhtPort), randomized(6223));
        BTorrentPort = new(db, nameof(TorrentPort), randomized(6323));
        BAuthInfo = new(db, nameof(AuthInfo), default);
        BNodeName = new(db, nameof(NodeName), null);

        DisabledTaskTypes = new(db, nameof(DisabledTaskTypes), ImmutableArray<TaskAction>.Empty);
        AcceptTasks = new(db, nameof(AcceptTasks), true);
        TaskAutoDeletionDelayDays = new(db, nameof(TaskAutoDeletionDelayDays), 4);
        BenchmarkResult = new(db, nameof(BenchmarkResult), default);
        TaskProcessingDirectory = new(db, nameof(TaskProcessingDirectory), default);


        foreach (var bindable in new[] { BLocalListenPort, BUPnpPort, BUPnpServerPort, BDhtPort, BTorrentPort })
            bindable.Save();
    }


    public readonly record struct BenchmarkInfo(Version Version, BenchmarkData Data);
}
