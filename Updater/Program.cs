using System.Diagnostics;
using System.Runtime.InteropServices;
using Common;
using Newtonsoft.Json.Linq;
using NLog;
using UpdaterCommon;



if (Initializer.UseAdminRights && RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
{
    // admin rights test
    try { File.Create(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "_test")).Dispose(); }
    catch
    {
        Elevate();
        return;
    }


    void Elevate()
    {
        var start = new ProcessStartInfo(Environment.ProcessPath!)
        {
            UseShellExecute = true,
            Verb = "runas",
        };
        foreach (var arg in args) start.ArgumentList.Add(arg);

        Process.Start(start).ThrowIfNull();
    }
}


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

if (doupdate)
{
    checker.MoveFilesAndStartApp();
    return;
}


while (true)
{
    var update = await checker.Update();
    update.LogIfError("Caught an error while updating: {0}; Restarting in a second...");
    if (update)
    {
        if (!FileList.IsProcessRunning(FileList.GetNodeExe()))
            Process.Start(FileList.GetNodeExe()).ThrowIfNull($"Process is null after starting node");

        if (!FileList.IsProcessRunning(FileList.GetNodeUIExe()))
            Process.Start(FileList.GetNodeUIExe()).ThrowIfNull($"Process is null after starting nodeui");

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