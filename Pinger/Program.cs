using System.Diagnostics;
using System.Globalization;
using Common;

if (args.Length < 1) throw new Exception("Node executable was not provided");

var nodeexe = args[0];
if (!File.Exists(nodeexe))
    throw new Exception(@$"File {nodeexe} does not exists");

Log(@$"Pinger started");
Log(@$"Config directory: {Variables.ConfigDirectory}");
Log(@$"Node executable: {nodeexe}");

AppendStartSession();
await Ping(new HttpClient(), CancellationToken.None).ConfigureAwait(false);


static void Log(string text) => Console.WriteLine(DateTimeOffset.Now + ": " + text);

static string GetCurrentTimeStr() => DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture);
void AppendStartSession() => AppendSession("+" + GetCurrentTimeStr());
void AppendEndSession() => AppendSession("-" + GetCurrentTimeStr());
void AppendSession(string text) => File.AppendAllText(Path.Combine(Variables.ConfigDirectory, "sessions"), text + "\n");

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
    var exepath = Path.GetFullPath(nodeexe);
    foreach (var p in Process.GetProcesses().Where(filter))
        p.Kill();

    AppendEndSession();
    _ = Process.Start(exepath) ?? throw new Exception("Could not start node process");
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