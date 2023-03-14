namespace Node;

public static class TorrentClientInstance
{
    public static readonly TorrentClient Instance = new(Settings.DhtPort, Settings.TorrentPort);
}
