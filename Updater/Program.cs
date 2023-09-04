using System.Diagnostics;
using System.Runtime.InteropServices;
using Common;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using NLog;
using UpdaterCommon;


ElevateIfNeeded();
Init.Initialize();

UpdateChecker checker;
var doupdate = false;
if (args.Length != 0)
{
    if (args[0] == "doupdate")
    {
        doupdate = true;
        checker = UpdateChecker.LoadFromJsonOrDefault();
    }
    else
    {
        var json = JObject.Parse(args[0]);

        doupdate = json["doupdate"]?.Value<bool>() ?? false;
        checker = json.ToObject<UpdateChecker>().ThrowIfNull("Invalid JSON argument");
    }
}
else checker = UpdateChecker.LoadFromJsonOrDefault();

LogManager.GetCurrentClassLogger().Info($"[{(doupdate ? "D" : null)}Updater] App: {checker.App}; Target dir: {checker.TargetDirectory}; Current executable: {Path.GetFullPath(Environment.ProcessPath!)}");

{
    Console.Title = "Renderfin";
    const string appname = $"Renderfin —";
    long allfiles = 0;
    long filesdownloaded = 0;

    checker.FetchingStarted += () => Console.Title = $"{appname} Fetching files";
    checker.FilteringStarted += () => Console.Title = $"{appname} Filtering changed files";
    checker.DownloadingStarted += files =>
    {
        allfiles = files.Sum(f => f.Size);
        Console.Title = $"{appname} Downloading files";
    };
    checker.BytesDownloaded += bytes => { filesdownloaded += bytes; Console.Title = $"{appname} Downloading files ({filesdownloaded * 100 / allfiles}%)"; };
    checker.StartingApp += () => Console.Title = $"{appname} Starting apps";
}


if (doupdate)
{
    checker.MoveFilesAndStartApp();
    return;
}


while (true)
{
    var update = await checker.Update();
    var logger = new NLog.Extensions.Logging.NLogLoggerFactory().CreateLogger<Program>();
    update.LogIfError(logger, "Caught an error while updating: {0}; Restarting in a second...");
    if (update)
    {
        if (!FileList.IsProcessRunning(FileList.GetNodeExe()))
            Process.Start(new ProcessStartInfo(FileList.GetNodeExe()) { WorkingDirectory = Path.GetDirectoryName(FileList.GetNodeExe()) })
                .ThrowIfNull($"Process is null after starting node");

        // UI will quit itself if already running, and will also trigger an existing one to be shown, so we skip the check here
        Process.Start(new ProcessStartInfo(FileList.GetNodeUIExe()) { WorkingDirectory = Path.GetDirectoryName(FileList.GetNodeUIExe()) })
            .ThrowIfNull($"Process is null after starting nodeui");

        return;
    }

    Thread.Sleep(1000);
}



/*async ValueTask RedownloadSelf()
{
    var updater = new UpdateChecker(url, apptype);

    var file = "Updater";
    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) file += ".exe";

    await updater.DownloadFile(targetdir, file, null, CancellationToken.None).ConfigureAwait(false);
    RestartToUpdatedSelf();
}*/


void ElevateIfNeeded()
{
    if (!Initializer.UseAdminRights || !RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return;

    // admin rights test
    try { File.Create(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "_test")).Dispose(); }
    catch { Elevate(); }


    void Elevate()
    {
        var start = new ProcessStartInfo(Environment.ProcessPath!)
        {
            UseShellExecute = true,
            Verb = "runas",
        };
        foreach (var arg in args) start.ArgumentList.Add(arg);

        Process.Start(start).ThrowIfNull();
        Environment.Exit(0);
    }
}