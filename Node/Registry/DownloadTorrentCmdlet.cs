using System.Management.Automation;
using MonoTorrent;
using MonoTorrent.BEncoding;
using MonoTorrent.Client;

namespace Node.Registry;

[Cmdlet("Download", "Torrent")]
public class DownloadTorrentCmdlet : PSCmdlet
{
    [Parameter(Position = 0, Mandatory = true)]
    public string Name { get; set; } = null!;

    [Parameter(Position = 1, Mandatory = true)]
    public string TargetPath { get; set; } = null!;

    protected override void ProcessRecord()
    {
        base.ProcessRecord();
        ProcessAsync().Wait();
    }
    async Task ProcessAsync()
    {
        Console.WriteLine("DOWNLOADING TORRENT " + Name + " TO " + TargetPath);
        var peer = (await LocalApi.Send<JsonPeer>(Settings.RegistryUrl, "getpeer")).ThrowIfError();

        using var torrentstreamcopy = new MemoryStream();
        using (var torrentstream = await new HttpClient().GetStreamAsync($"{Settings.RegistryUrl}/gettorrent?plugin={Name}"))
            await torrentstream.CopyToAsync(torrentstreamcopy);

        torrentstreamcopy.Position = 0;
        var manager = await TorrentClient.AddOrGetTorrent(await Torrent.LoadAsync(torrentstreamcopy), TargetPath); // TODO: move IP declaration into registry or something 
        await manager.AddPeerAsync(new Peer(BEncodedString.FromUrlEncodedString(peer.PeerId), new Uri("ipv4://135.125.237.7:" + peer.Port)));
        // " + Settings.RegistryUrl.Split('/')[^1].Split(':')[0] + "

        while (true)
        {
            await Task.Delay(2000);
            if (manager.Progress == 100 || manager.State == TorrentState.Seeding)
                break;
        }
        await manager.StopAsync(TimeSpan.FromSeconds(10));
    }
}
