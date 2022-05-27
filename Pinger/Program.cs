using System.Diagnostics;
using System.Globalization;
using Common;


var host = Host.CreateDefaultBuilder(args)
    .UseWindowsService()
    .UseSystemd()
    .ConfigureServices(services => services.AddHostedService<Worker>())
    .Build();

await host.RunAsync();


class Worker : BackgroundService
{
    readonly string SessionsFile = Path.Combine(Variables.ConfigDirectory, "sessions");
    readonly string NodeExecutable;

    readonly ILogger<Worker> Logger;

    public Worker(ILogger<Worker> logger)
    {
        Logger = logger;

        var args = Environment.GetCommandLineArgs();
        if (args.Length < 2) throw new Exception("Node executable was not provided");

        NodeExecutable = args[1];
        if (!File.Exists(NodeExecutable)) throw new Exception(@$"File {NodeExecutable} does not exists");
    }


    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Log(@$"Pinger started");
        Log(@$"Config directory: {Variables.ConfigDirectory}");
        Log(@$"Node executable: {NodeExecutable}");

        AppendStartSession();

        while (true)
        {
            await Task.Delay(60 * 1000).ConfigureAwait(false);
            await Ping(new HttpClient(), CancellationToken.None).ConfigureAwait(false);
        }
    }

    void Log(string text) => Logger.LogInformation(text);


    static string CurrentTimeStr => DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture);
    void AppendStartSession() => AppendSession("+" + CurrentTimeStr);
    void AppendEndSession() => AppendSession("-" + CurrentTimeStr);
    void AppendSession(string text) => File.AppendAllText(SessionsFile, text + "\n");

    async Task<bool> Ping(HttpClient client, CancellationToken token, int tries = 0)
    {
        if (tries >= 3) throw new Exception(@$"Application does not respond after {tries} restarts");

        var sw = Stopwatch.StartNew();
        Log(@$"Sending ping ({tries + 1}/3)...");

        HttpResponseMessage msg;
        try { msg = await client.GetAsync(@$"http://127.0.0.1:{Settings.ListenPort}/ping", token).ConfigureAwait(false); }
        catch (Exception ex) { return await restart(@$"Could not connect to the node, {ex.Message}, restarting the node...").ConfigureAwait(false); }

        if (!msg.IsSuccessStatusCode)
            return await restart(@$"Ping bad, HTTP {msg.StatusCode}, restarting the node...").ConfigureAwait(false);

        Log(@$"Ping good ({sw.ElapsedMilliseconds}ms)...");
        return true;


        Task<bool> restart(string message) => resend(5000, true, message);
        async Task<bool> resend(int delay, bool restart, string message)
        {
            Log(message);
            if (restart) RestartNode();

            await Task.Delay(delay, token).ConfigureAwait(false);
            return await Ping(client, token, tries + 1).ConfigureAwait(false);
        }
    }
    void RestartNode()
    {
        var exepath = Path.GetFullPath(NodeExecutable);
        foreach (var p in Process.GetProcesses().Where(filter))
            p.Kill();

        Process.Start(exepath);
        AppendEndSession();
        AppendStartSession();


        bool filter(Process proc)
        {
            try
            {
                var module = proc.MainModule;
                if (module?.FileName is null) return false;

                return Path.GetFullPath(module.FileName) == exepath;
            }
            catch { return false; }
        }
    }
}