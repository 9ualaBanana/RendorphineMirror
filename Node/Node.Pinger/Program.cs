using Node.Common;
using UpdaterCommon;


await Init.For(new Init.InitConfig("renderfin")).ExecuteAsync();

var updater = UpdateChecker.LoadFromJsonOrDefault(args: new Dictionary<string, string>() { ["Node.UI"] = "hidden" });
await updater.Update().ThrowIfError();


try
{
    var portfile = new[] { Directories.DataFor("renderfin"), Directories.Data, }
        .Select(p => Path.Combine(p, "lport"))
        .First(File.Exists);

    var port = ushort.Parse(await File.ReadAllTextAsync(portfile));

    var msg = await new HttpClient().GetAsync($"http://127.0.0.1:{port}/ping");
    if (!msg.IsSuccessStatusCode)
        restart($"Ping bad, HTTP {msg.StatusCode}, restarting...");
}
catch (Exception ex)
{
    restart($"Could not connect to the node: {ex.Message}, starting...");
}


void restart(string errmsg)
{
    NLog.LogManager.GetCurrentClassLogger().Info(errmsg);

    FileList.KillProcesses();
    updater.StartApp();
}