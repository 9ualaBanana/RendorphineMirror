using System.Management.Automation;

namespace Node.Registry;

[Cmdlet("Download", "Plugin-Torrent")]
public class DownloadPluginTorrentCmdlet : PSCmdlet
{
    [Parameter(Position = 0, Mandatory = true)]
    public DownloadTarget Target { get; set; }

    protected override void ProcessRecord()
    {
        base.ProcessRecord();


        var pltorrent = this.GetVariableValue("PLTORRENT").ToString()!;
        var dir = Target switch
        {
            DownloadTarget.Downloads => this.GetVariableValue("PLDOWNLOAD").ToString()!,
            DownloadTarget.Plugins => this.GetVariableValue("PLINSTALL").ToString()!,
            _ => throw new InvalidOperationException("Invalid target"),
        };

        Directory.CreateDirectory(dir);
        DownloadTorrentCmdlet.Download(pltorrent, dir).Wait();
    }


    public enum DownloadTarget
    {
        Downloads,
        Plugins,
    }
}
