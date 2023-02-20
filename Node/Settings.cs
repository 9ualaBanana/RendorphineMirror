namespace Node;

public static class Settings
{
    public static event Action? AnyChanged;

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

    static Settings()
    {
        static ushort randomized(ushort port) => (ushort) (port + Random.Shared.Next(80));

        var db = Database.Instance;

        BServerUrl = new(db, nameof(ServerUrl), "https://t.microstock.plus:8443");
        BLocalListenPort = new(db, nameof(LocalListenPort), randomized(5123));
        BUPnpPort = new(db, nameof(UPnpPort), randomized(5223));
        BUPnpServerPort = new(db, nameof(UPnpServerPort), randomized(5323));
        BDhtPort = new(db, nameof(DhtPort), randomized(6223));
        BTorrentPort = new(db, nameof(TorrentPort), randomized(6323));
        BAuthInfo = new(db, nameof(AuthInfo), default);
        BNodeName = new(db, nameof(NodeName), null);

        typeof(Settings).GetFields(System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public)
            .Where(x => x.FieldType.IsAssignableTo(typeof(IBindable)))
            .Select(x => ((IBindable) x.GetValue(null)!, x.Name))
            .ToList()
            .ForEach(x => x.Item1.Changed += () => AnyChanged?.Invoke());

        foreach (var bindable in new[] { BLocalListenPort, BUPnpPort, BUPnpServerPort, BDhtPort, BTorrentPort })
            bindable.Save();
    }
}