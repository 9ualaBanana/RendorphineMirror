using System.Diagnostics;
using System.Management.Automation;

namespace Node.Registry;

[Cmdlet("Download", "Run-Installer")]
public class DownloadRunInstallerCmdlet : PSCmdlet
{
    static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    [Parameter(Position = 0, Mandatory = true)]
    public string Url { get; set; } = null!;

    [Parameter(Mandatory = false)]
    public string? Args { get; set; }

    protected override void ProcessRecord()
    {
        base.ProcessRecord();


        var downloaddir = this.GetVariableValue("PLDOWNLOAD").ToString()!;
        var installer = Path.Combine(downloaddir, "install.exe");

        Logger.Info($"Downloading {Url} into {installer}");
        using (var stream = new HttpClient().GetStreamAsync(Url).Result)
        using (var installfile = File.OpenWrite(installer))
            stream.CopyTo(installfile);

        Logger.Info($"Installer downloaded, executing with args '{Args}'");
        var process = Process.Start(new ProcessStartInfo(installer, Args ?? string.Empty) { RedirectStandardOutput = true })
            .ThrowIfNull("Could not start the process");

        new Thread(() =>
        {
            var reader = process.StandardOutput;
            while (!process.HasExited)
            {
                var line = reader.ReadLine();
                Logger.Info($"[{process.Id}] {line}");
            }
        })
        { IsBackground = true }.Start();

        process.WaitForExit();
    }
}
