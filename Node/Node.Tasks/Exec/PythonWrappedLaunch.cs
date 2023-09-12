namespace Node.Tasks.Exec;

/// <summary> Launcher for Nikolaj's wrappers over python AI apps. Not reusable, creatre new instance for every invocation. </summary>
public abstract class PythonWrappedLaunch
{
    public required CondaInvoker CondaInvoker { get; init; }
    public required PluginList Plugins { get; init; }
    public required IProgressSetter ProgressSetter { get; init; }
    protected ILogger Logger { get; }

    protected abstract PluginType PluginType { get; }

    protected PythonWrappedLaunch(ILogger logger) => Logger = logger;

    protected virtual bool IsInstalled()
    {
        var file = Path.Combine(Plugins.GetPlugin(PluginType).Path, "..", "tmp", "installed");
        return File.Exists(file) && File.ReadAllText(file).Trim() == "true";
    }

    async Task InstallAsync()
    {
        // TODO: move installation to installation not execution
        Logger.LogInformation("Installing...");

        await CondaInvoker.ExecutePowerShellAtWithCondaEnvAsync(
            Plugins,
            PluginType,
            @$"""{Plugins.GetPlugin(PluginType).Path}"" install",
            null
        );

        Logger.LogInformation("Installed.");
    }

    protected async Task<ProcessLauncher> CreateLauncherAsync()
    {
        if (!IsInstalled())
            await InstallAsync();

        return new ProcessLauncher(Plugins.GetPlugin(PluginType).Path)
        {
            ThrowOnStdErr = false,
            Logging = { ILogger = Logger },
            Timeout = TimeSpan.FromMinutes(10),
        }
            .AddOnOut(onread)
            .AddOnErr(onerr);


        void onread(string line)
        {
            if (line.StartsWith("Unhandled exception: ", StringComparison.Ordinal)
                || line.ContainsOrdinal("was not matched. Did you mean one of the following?"))
                throw new Exception(line);

            if (line.StartsWith("Progress: ", StringComparison.Ordinal))
            {
                var progress = int.Parse(line.AsSpan()["Progress: ".Length..^"%".Length], CultureInfo.InvariantCulture) / 100d;
                ProgressSetter.Set(progress);
                return;
            }
        }
        void onerr(string line)
        {
            if (line == "Process not running, starting") return;

            throw new Exception(line);
        }
    }
}
