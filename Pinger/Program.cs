using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Common;

ConsoleHide.Hide();

var nodeexe = GetPath(args, 0, "Node");
var updaterexe = GetPath(args, 1, "Updater");

Log(@$"Pinger v{Init.Version} started");
Log(@$"Config directory: {Init.ConfigDirectory}");
Log(@$"Node executable: {nodeexe}");
Log(@$"Updater executable: {updaterexe}");

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

async Task<bool> Ping(HttpClient client, CancellationToken token)
{
    if (!Process.GetProcesses().Any(proc => Filter(proc, fname => fname == updaterexe)))
        restart(@$"Updater process was not found, starting...");

    var sw = Stopwatch.StartNew();
    Log(@$"Sending ping...");

    HttpResponseMessage msg;
    try { msg = await client.GetAsync(@$"http://127.0.0.1:{Settings.LocalListenPort}/ping", token).ConfigureAwait(false); }
    catch (Exception ex) { restart(@$"Could not connect to the node, {ex.Message}, starting..."); }

    if (!msg.IsSuccessStatusCode)
        restart(@$"Ping bad, HTTP {msg.StatusCode}, restarting...");

    Log(@$"Ping good ({sw.ElapsedMilliseconds}ms)...");
    return true;


    [DoesNotReturn]
    void restart(string message)
    {
        Log(message);
        RestartNode();

        Environment.Exit(0);
        throw new InvalidOperationException();
    }
}
void RestartNode()
{
    AppendEndSession();
    FileList.KillProcesses();

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