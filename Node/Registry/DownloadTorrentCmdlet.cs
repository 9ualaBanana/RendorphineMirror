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
        Download(Name, TargetPath).Wait();
    }


    public static async Task Download(string name, string targetdir)
    {
        Console.WriteLine("DOWNLOADING TORRENT " + name + " TO " + targetdir);
        var peer = (await Api.Default.ApiGet<JsonPeer>($"{Apis.RegistryUrl}/getpeer", "value", "Getting torrent peer")).ThrowIfError();

        using var torrentstreamcopy = new MemoryStream();
        using (var torrentstream = await new HttpClient().GetStreamAsync($"{Apis.RegistryUrl}/gettorrent?plugin={name}"))
            await torrentstream.CopyToAsync(torrentstreamcopy);

        torrentstreamcopy.Position = 0;
        var manager = await TorrentClient.AddOrGetTorrent(await Torrent.LoadAsync(torrentstreamcopy), targetdir);

        // TODO:                                                                                     move IP declaration into registry or something 
        foreach (var port in peer.Ports)
            await manager.AddPeerAsync(new Peer(BEncodedString.FromUrlEncodedString(peer.PeerId), new Uri("ipv4://135.125.237.7:" + port)));
        // " + Apis.RegistryUrl.Split('/')[^1].Split(':')[0] + "

        while (true)
        {
            await Task.Delay(2000);
            if (manager.Progress == 100 || manager.State == TorrentState.Seeding)
                break;
        }
        await manager.StopAsync(TimeSpan.FromSeconds(10));
    }
}
