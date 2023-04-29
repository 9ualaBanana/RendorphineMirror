using UpdaterCommon;


Init.Initialize();
var updater = UpdateChecker.LoadFromJsonOrDefault(args: new Dictionary<string, string>() { ["NodeUI"] = "hidden" });
await updater.Update().ThrowIfError();


try
{
    var portfile = Path.Combine(Directories.Data, "lport");
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
    LogManager.GetCurrentClassLogger().Info(errmsg);

    FileList.KillProcesses();
    updater.StartApp();
}