using Common;
using NLog;
using NodeCommon;
using UpdaterCommon;


Init.Initialize();
var updater = UpdateChecker.LoadFromJsonOrDefault();
await updater.Update().ThrowIfError();


try
{
    var msg = await new HttpClient().GetAsync($"http://127.0.0.1:{Settings.LocalListenPort}/ping");
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