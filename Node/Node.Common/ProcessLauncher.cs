using System.Text;

namespace Node.Common;

public class ProcessLauncher
{
    event Action<Process, bool, string>? OnRead;

    readonly string Executable;

    public ArgList Arguments { get; } = new();
    public bool WineSupport { get; init; } = false;
    public bool ThrowOnStdErr { get; init; } = true;
    public bool ThrowOnNonZeroExitCode { get; init; } = true;
    public ProcessLogging Logging { get; init; } = new();
    StringBuilder? StringBuilder;

    public ProcessLauncher(string executable) => Executable = executable;
    public ProcessLauncher(string executable, params string?[] args) : this(executable, args.AsEnumerable()) { }
    public ProcessLauncher(string executable, IEnumerable<string?>? args) : this(executable) => Arguments.Add(args);


    public ProcessLauncher WithArgs(Action<ArgList> func)
    {
        func(Arguments);
        return this;
    }

    ProcessLauncher EnableBuilder(out StringBuilder result)
    {
        result = (StringBuilder ??= new());
        return this;
    }

    public ProcessLauncher AddOnErr(Action<string> func) => AddOnRead(true, func);
    public ProcessLauncher AddOnOut(Action<string> func) => AddOnRead(false, func);
    public ProcessLauncher AddOnRead(bool err, Action<string> func) =>
        AddOnRead((readerr, line) =>
        {
            if (readerr == err)
                func(line);
        });
    public ProcessLauncher AddOnRead(Action<bool, string> func)
    {
        OnRead += (proc, err, line) => func(err, line);
        return this;
    }

    static string WrapWithSpaces(string str) => str.Contains(' ', StringComparison.Ordinal) ? $"\"{str}\"" : str;


    Process Start(out Task readingtask)
    {
        var procinfo = new ProcessStartInfo(WineSupport ? "wine" : Executable)
        {
            WindowStyle = ProcessWindowStyle.Hidden,
            CreateNoWindow = true,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };

        if (WineSupport)
            procinfo.ArgumentList.Add(Executable);
        foreach (var arg in Arguments)
            procinfo.ArgumentList.Add(arg);

        Logging.Logger?.LogInfo($"Starting {WrapWithSpaces(procinfo.FileName)} {string.Join(' ', procinfo.ArgumentList.Select(WrapWithSpaces))}");
        var process = new Process() { StartInfo = procinfo };

        process.Start();
        readingtask = Task.WhenAll(
            startReading(process, true),
            startReading(process, false)
        );

        return process;


        async Task startReading(Process process, bool err)
        {
            try
            {
                var stream = err ? process.StandardError : process.StandardOutput;

                while (true)
                {
                    var line = await stream.ReadLineAsync();
                    if (line is null) return;

                    if (err && ThrowOnStdErr)
                        throw new Exception(line);

                    Logging.Logger?.Log(err ? Logging.StdErr : Logging.StdOut, $"[Process {process.Id}] {line}");
                    StringBuilder?.AppendLine(line);
                    OnRead?.Invoke(process, err, line);
                }
            }
            catch
            {
                process.Kill();
                throw;
            }
        }
    }

    /// <summary> Start the process, wait for exit </summary>
    public async Task ExecuteAsync()
    {
        using var proc = Start(out var readingtask);
        await proc.WaitForExitAsync();
        await readingtask;

        if (ThrowOnNonZeroExitCode)
            EnsureZeroExitCode(proc);
    }

    /// <summary> Start the process, wait for exit, force kill after <paramref name="cancelAfter"/> </summary>
    public async Task ExecuteAsync(TimeSpan cancelAfter)
    {
        using var proc = Start(out var readingtask);

        var token = new CancellationTokenSource();
        token.CancelAfter(cancelAfter);
        var kill = token.Token.UnsafeRegister((_, _) =>
        {
            try
            {
                if (!proc.HasExited)
                    proc.Kill();
            }
            catch { }
        }, null);

        await proc.WaitForExitAsync();
        kill.Unregister();
        await readingtask;

        if (ThrowOnNonZeroExitCode)
            EnsureZeroExitCode(proc);
    }

    /// <inheritdoc cref="ExecuteFullAsync"/>
    public string ExecuteFull() => ExecuteFullAsync().GetAwaiter().GetResult();

    /// <summary> Start the process, wait for exit, return full output </summary>
    public async Task<string> ExecuteFullAsync()
    {
        EnableBuilder(out var builder);
        await ExecuteAsync();

        return toStringTrimmed(builder);


        /// <summary> Trim StringBuilder without allocating an unnesessary and potentially big string </summary>
        static string toStringTrimmed(StringBuilder builder)
        {
            var startidx = 0;
            var endidx = builder.Length - 1;

            while (char.IsWhiteSpace(builder[startidx]))
                startidx++;
            while (char.IsWhiteSpace(builder[endidx]))
                endidx--;

            return builder.ToString(startidx, endidx - startidx + 1);
        }
    }


    static void EnsureZeroExitCode(Process process)
    {
        if (process.ExitCode != 0)
            throw new NodeProcessException(process, $"Process {process.Id} ended with exit code {process.ExitCode}");
    }

    /// <summary>
    /// For windows, just returns provided path parameter.
    /// For unix, returns path for use in wine: /home/user/test -> Z:\home\user\test
    /// </summary>
    public static async Task<string> GetWinPath(string path)
    {
        if (Environment.OSVersion.Platform == PlatformID.Win32NT) return path;

        return await new ProcessLauncher("/bin/winepath", "-w", path)
            .ExecuteFullAsync();
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


    public class ProcessLogging
    {
        public ILoggable? Logger { get; set; }
        public LogLevel StdOut { get; set; } = LogLevel.Trace;
        public LogLevel StdErr { get; set; } = LogLevel.Error;
    }
}