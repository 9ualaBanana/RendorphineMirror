using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Common;
using NLog;

try { ConsoleHide.Hide(); }
catch { }

try { Process.Start(new ProcessStartInfo(FileList.GetUpdaterExe()) { CreateNoWindow = true, WindowStyle = ProcessWindowStyle.Hidden })!.WaitForExit(); }
catch (Exception ex)
{
    try { File.AppendAllText(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "renderfinpinger", "exception"), ex.ToString()); }
    catch { }
}

Init.Initialize();
var _logger = LogManager.GetCurrentClassLogger();

var nodeexe = GetPath(args, 0, "Node");
var updaterexe = GetPath(args, 1, "Updater");

await Ping(new HttpClient(), CancellationToken.None).ConfigureAwait(false);


static string GetPath(string[] args, int index, string info)
{
    if (args.Length < index + 1) throw new Exception(info + " executable was not provided");

    var path = args[index];
    if (!File.Exists(path))
        throw new Exception(@$"{info} executable {path} does not exists");

    return Path.GetFullPath(path);
}

void Log(string text) => _logger.Info(text);

async Task<bool> Ping(HttpClient client, CancellationToken token)
{
    var sw = Stopwatch.StartNew();

    HttpResponseMessage msg;
    try { msg = await client.GetAsync(@$"http://127.0.0.1:{Settings.LocalListenPort}/ping", token).ConfigureAwait(false); }
    catch (Exception ex) { restart(@$"Could not connect to the node, {ex.Message}, starting..."); }

    if (!msg.IsSuccessStatusCode)
        restart(@$"Ping bad, HTTP {msg.StatusCode}, restarting...");

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
    FileList.KillProcesses();
    _ = Process.Start(new ProcessStartInfo(updaterexe) { CreateNoWindow = true, WindowStyle = ProcessWindowStyle.Hidden }) ?? throw new Exception("Could not start updater process");
}