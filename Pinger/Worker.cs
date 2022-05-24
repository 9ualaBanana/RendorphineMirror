using System.Diagnostics;

namespace Pinger;

public class Worker : BackgroundService
{
    readonly HttpClient Client = new();
    readonly ILogger<Worker> Logger;

    public Worker(ILogger<Worker> logger)
    {
        Logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
            await Ping(token);
    }


    Task Ping(CancellationToken token)
    {
        return ping(0);


        async Task<bool> ping(int tries)
        {
            if (tries >= 3) throw new Exception(@$"Application does not respond after {tries} restarts");

            Logger.LogInformation(@$"{DateTimeOffset.Now}: Sending ping ({tries + 1}/3)...");

            HttpResponseMessage msg;
            try { msg = await Client.GetAsync(@$"http://127.0.0.1:{Settings.ListenPort}/ping", token).ConfigureAwait(false); }
            catch (Exception ex) { return await restart(@$"{DateTimeOffset.Now}: Could not connect to the node, {ex.Message}, restarting the node...").ConfigureAwait(false); }

            if (!msg.IsSuccessStatusCode)
                return await restart(@$"{DateTimeOffset.Now}: Ping bad, HTTP {msg.StatusCode}, restarting the node...").ConfigureAwait(false);

            var wait = Settings.PingDelaySec;
            Logger.LogInformation(@$"{DateTimeOffset.Now}: Ping gud, waiting for {wait}s");
            await Task.Delay(wait * 1000, token);
            return true;


            Task<bool> restart(string message) => resend(5000, true, message);
            async Task<bool> resend(int delay, bool restart, string message)
            {
                Logger.LogInformation(message);
                if (restart) RestartNode();

                await Task.Delay(delay, token).ConfigureAwait(false);
                return await ping(tries + 1).ConfigureAwait(false);
            }
        }
    }
    void RestartNode()
    {
        var exepath = Path.GetFullPath(Settings.NodeExePath);
        foreach (var p in getprocesses())
            p.Kill();

        Process.Start(exepath);


        IEnumerable<Process> getprocesses()
        {
            foreach (var proc in Process.GetProcesses())
            {
                if (proc.ProcessName is ("renderphine" or "renderphine.exe"))
                {
                    yield return proc;
                    continue;
                }

                try
                {
                    var module = proc.MainModule;
                    if (module?.FileName is null) continue;

                    if (Path.GetFullPath(module.FileName) == exepath)
                        yield return proc;
                }
                finally { }
            }
        }
    }
}
