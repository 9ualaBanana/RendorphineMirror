using System.Diagnostics;
using System.Globalization;
using Common;

ConsoleHide.Hide();

var nodeexe = GetPath(args, 0, "Node");
var updaterexe = GetPath(args, 1, "Updater");

Log(@$"Pinger started");
Log(@$"Config directory: {Init.ConfigDirectory}");
Log(@$"Node executable: {nodeexe}");
Log(@$"Updater executable: {updaterexe}");

AppendStartSession();
await Ping(new HttpClient(), CancellationToken.None).ConfigureAwait(false);


static string GetPath(string[] args, int index, string info)
{
    if (args.Length < index + 1) throw new Exception(info + " executable was not provided");

    var path = args[index];
    if (!File.Exists(path))
        throw new Exception(@$"{info} executable {path} does not exists");

    return Path.GetFullPath(path);
}

static void Log(string text) => Console.WriteLine(DateTimeOffset.Now + ": " + text);

static string GetCurrentTimeStr() => DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture);
void AppendStartSession() => AppendSession("+" + GetCurrentTimeStr());
void AppendEndSession() => AppendSession("-" + GetCurrentTimeStr());
void AppendSession(string text) => File.AppendAllText(Path.Combine(Init.ConfigDirectory, "sessions"), text + "\n");

async Task<bool> Ping(HttpClient client, CancellationToken token, int tries = 0)
{
    if (tries >= 3) throw new Exception(@$"Application does not respond after {tries} restarts");


    if (!Process.GetProcesses().Any(proc => Filter(proc, fname => fname == updaterexe)))
        return await restart(@$"Updater process was not found, starting...").ConfigureAwait(false);

    var sw = Stopwatch.StartNew();
    Log(@$"Sending ping ({tries + 1}/3)...");

    HttpResponseMessage msg;
    try { msg = await client.GetAsync(@$"http://127.0.0.1:{Settings.ListenPort}/ping", token).ConfigureAwait(false); }
    catch (Exception ex) { return await restart(@$"Could not connect to the node, {ex.Message}, starting...").ConfigureAwait(false); }

    if (!msg.IsSuccessStatusCode)
        return await restart(@$"Ping bad, HTTP {msg.StatusCode}, restarting...").ConfigureAwait(false);

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
    foreach (var p in Process.GetProcesses().Where(proc => Filter(proc, fpath => fpath == nodeexe || fpath == updaterexe)))
        p.Kill();

    AppendEndSession();
    _ = Process.Start(updaterexe) ?? throw new Exception("Could not start updater process");
    AppendStartSession();
}
static bool Filter(Process proc, Func<string, bool> check)
{
    try
    {
        var module = proc.MainModule;
        if (module?.FileName is null) return false;

        var fpath = Path.GetFullPath(module.FileName);
        return check(fpath);
    }
    catch { return false; }
}