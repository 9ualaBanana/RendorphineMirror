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

        using var torrentstreamcopy = new MemoryStream();
        using (var torrentstream = await new HttpClient().GetStreamAsync($"{Apis.RegistryUrl}/gettorrent?plugin={name}"))
            await torrentstream.CopyToAsync(torrentstreamcopy);

        torrentstreamcopy.Position = 0;
        var manager = await TorrentClientInstance.Instance.AddOrGetTorrent(await Torrent.LoadAsync(torrentstreamcopy), targetdir);

        while (true)
        {
            await Task.Delay(2000);
            if (manager.Progress == 100 || manager.State == TorrentState.Seeding)
                break;
        }
        await manager.StopAsync(TimeSpan.FromSeconds(10));
    }
}
