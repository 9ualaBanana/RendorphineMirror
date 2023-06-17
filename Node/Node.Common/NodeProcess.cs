using System.Diagnostics;
using System.Text;

namespace Node.Common;

public record NodeProcess(string Executable, List<string> Arguments, ILoggable? Logger, Action<bool, string>? OnRead = null, LogLevel? StdOut = null, LogLevel? StdErr = null)
{
    private NodeProcess(string executable, List<string> arguments, ILoggable? logger) : this(executable, arguments, logger, null, null, null) { }
    public NodeProcess(string executable, ILoggable? logger, params string[] arguments) : this(executable, arguments.ToList(), logger) { }
    public NodeProcess(string executable, IEnumerable<string> arguments, ILoggable? logger, Action<bool, string>? onread = null, LogLevel? stdout = null, LogLevel? stderr = null)
        : this(executable, arguments.ToList(), logger, onread, stdout, stderr) { }


    Process Start()
    {
        Logger?.LogInfo($"Starting {Executable} {string.Join(' ', Arguments.Select(arg => arg.Contains(' ', StringComparison.Ordinal) ? $"\"{arg}\"" : arg))}");

        var info = new ProcessStartInfo(Executable)
        {
            WindowStyle = ProcessWindowStyle.Hidden,
            CreateNoWindow = true,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        foreach (var arg in Arguments)
            info.ArgumentList.Add(arg);

        var process = Process.Start(info);
        if (process is null) throw new InvalidOperationException($"Could not start {Executable}");

        return process;
    }

    /// <summary> Starts a process, waits for exit, ensures the status code is 0 </summary>
    public async Task Execute()
    {
        using var process = Start();
        var reading = StartReadingOutput(process);

        await process.WaitForExitAsync();
        await reading;

        EnsureZeroStatusCode(process);
    }
    /// <summary> Starts a process, waits for exit, ensures the status code is 0, returns endtrimmed stdout </summary>
    public async Task<string> FullExecute()
    {
        var sdtr = new StringBuilder();
        await (this with { OnRead = onread }).Execute();
        return sdtr.ToString().TrimEnd();


        void onread(bool err, string data)
        {
            OnRead?.Invoke(err, data);
            if (!err) sdtr.AppendLine(data);
        }
    }


    /// <summary> Finds file in PATH and returns the full path, e.g. "python" => "/bin/python" </summary>
    public static OperationResult<string> FindInPath(string file)
    {
        try
        {
            var path = PowerShellInvoker.JustInvoke<string>($"(Get-Command '{file}').Path")[0];
            if (string.IsNullOrWhiteSpace(path)) return err();

            return path;
        }
        catch { return err(); }

        static OperationResult err() => OperationResult.Err("Command not found");
    }

    /// <summary>
    /// For windows, just returns provided path parameter.
    /// For unix, returns path for use in wine: /home/user/test -> Z:\home\user\test
    /// </summary>
    public static async ValueTask<string> GetWinPath(string path, ILoggable? logger)
    {
        if (Environment.OSVersion.Platform == PlatformID.Win32NT) return path;
        return await new NodeProcess("/bin/winepath", logger, new[] { "-w", path }).FullExecute();
    }

    /// <summary> Returns a copy of this NodeProcess but wrapped with wine if on unix </summary>
    public NodeProcess WithWineSupport()
    {
        if (Environment.OSVersion.Platform == PlatformID.Unix && Executable.EndsWith(".exe", StringComparison.Ordinal))
            return this with { Executable = "/bin/wine", Arguments = Arguments.Prepend(Executable).ToList() };

        return this;
    }


    static void EnsureZeroStatusCode(Process process)
    {
        if (process.ExitCode != 0)
            throw new NodeProcessException(process.ExitCode, $"Task process ended with exit code {process.ExitCode}");
    }
    Task StartReadingOutput(Process process)
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
                if (err) Logger?.Log(StdErr ?? LogLevel.Error, logstr);
                else Logger?.Log(StdOut ?? LogLevel.Info, logstr);

                OnRead?.Invoke(err, str);
            }
        }
    }



    public NodeProcess WithArgument(string arg)
    {
        Arguments.Add(arg);
        return this;
    }
    public NodeProcess WithArgument(params string[] args) => WithArgument(args.AsEnumerable());
    public NodeProcess WithArgument(IEnumerable<string> args)
    {
        foreach (var arg in args)
            Arguments.Add(arg);

        return this;
    }
}
