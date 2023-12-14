namespace NodeToUI;

public interface INodeSettings
{
    AuthInfo? AuthInfo { get; set; }
    string ServerUrl { get; set; }
    ushort LocalListenPort { get; set; }
    ushort UPnpPort { get; set; }
    ushort UPnpServerPort { get; set; }
    ushort DhtPort { get; set; }
    ushort TorrentPort { get; set; }
    string NodeName { get; set; }
}
