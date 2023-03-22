using System.Diagnostics;
using System.Text;

namespace Node;

public static class Processes
{
    static Process Start(string exepath, string args, IEnumerable<string> argsarr, ILoggable? logobj)
    {
        logobj?.LogInfo($"Starting {exepath} {args}{string.Join(' ', argsarr)}");

        var startinfo = new ProcessStartInfo(exepath, args)
        {
            WindowStyle = ProcessWindowStyle.Hidden,
            CreateNoWindow = true,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        foreach (var arg in argsarr) startinfo.ArgumentList.Add(arg);

        var process = Process.Start(startinfo);
        if (process is null) throw new InvalidOperationException("Could not start plugin process");

        return process;
    }

    static void EnsureZeroStatusCode(Process process)
    {
        if (process.ExitCode != 0)
            throw new Exception($"Task process ended with exit code {process.ExitCode}");
    }
    static Task StartReadingOutput(Process process, Action<bool, string>? onRead, ILoggable? logobj, LogLevel? stdout, LogLevel? stderr)
    {
        return Task.WhenAll(
            startReading(process.StandardOutput, false),
            startReading(process.StandardError, true)
        );


        async Task startReading(StreamReader input, bool err)
        {
            while (true)
            {
                var str = await input.ReadLineAsync().ConfigureAwait(false);
                if (str is null) return;

                var logstr = $"[Process {process.Id}] {str}";
                if (err) logobj?.Log(stderr ?? LogLevel.Error, logstr);
                else logobj?.Log(stdout ?? LogLevel.Info, logstr);

                onRead?.Invoke(err, str);
            }
        }
    }

    /// <summary> Starts a process, waits for exit, ensures the status code is 0 </summary>
    public static async Task Execute(string exepath, string args, IEnumerable<string> argsarr, Action<bool, string>? onRead, ILoggable? logobj, LogLevel? stdout, LogLevel? stderr)
    {
        using var process = Start(exepath, args, argsarr, logobj);
        var reading = StartReadingOutput(process, onRead, logobj, stdout, stderr);

        await process.WaitForExitAsync();
        await reading;

        EnsureZeroStatusCode(process);
    }
    /// <inheritdoc cref="Execute"/>
    public static Task Execute(string exepath, IEnumerable<string> args, Action<bool, string>? onRead, ILoggable? logobj, LogLevel? stdout = null, LogLevel? stderr = null) =>
        Execute(exepath, string.Empty, args, onRead, logobj, stdout, stderr);
    /// <inheritdoc cref="Execute"/>
    public static Task Execute(string exepath, string args, Action<bool, string>? onRead, ILoggable? logobj, LogLevel? stdout = null, LogLevel? stderr = null) =>
        Execute(exepath, args, Enumerable.Empty<string>(), onRead, logobj, stdout, stderr);


    /// <summary> Starts a process, waits for exit, ensures the status code is 0, returns stdout </summary>
    static async Task<string> FullExecute(string exepath, string args, IEnumerable<string> argsarr, ILoggable? logobj, LogLevel? stdout = null, LogLevel? stderr = null)
    {
        var sdtr = new StringBuilder();
        await Execute(exepath, args, argsarr, (err, data) => { if (!err) sdtr.Append(data); }, logobj, stdout, stderr);

        return sdtr.ToString();
    }
    /// <inheritdoc cref="FullExecute"/>
    public static Task<string> FullExecute(string exepath, string args, ILoggable? logobj, LogLevel? stdout = null, LogLevel? stderr = null) =>
        FullExecute(exepath, args, Enumerable.Empty<string>(), logobj, stdout, stderr);
    /// <inheritdoc cref="FullExecute"/>
    public static Task<string> FullExecute(string exepath, IEnumerable<string> args, ILoggable? logobj, LogLevel? stdout = null, LogLevel? stderr = null) =>
        FullExecute(exepath, string.Empty, args, logobj, stdout, stderr);
}
