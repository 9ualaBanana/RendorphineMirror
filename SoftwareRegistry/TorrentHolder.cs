namespace SoftwareRegistry;

public class TorrentHolder
{
    readonly Dictionary<string, byte[]> Torrents = new();

    static string ToKey(string plugin, string version) => $"{plugin}_{version}";

    public void Add(string plugin, string version, byte[] bytes) =>
        Torrents.Add(ToKey(plugin, version), bytes);

    public void Set(string plugin, string version, byte[] bytes) =>
        Torrents[ToKey(plugin, version)] = bytes;

    public void Remove(string plugin, string version) =>
        Torrents.Remove(ToKey(plugin, version));

    public bool TryGet(string plugin, string version, [NotNullWhen(true)] out byte[]? bytes) =>
        Torrents.TryGetValue(ToKey(plugin, version), out bytes);
}
