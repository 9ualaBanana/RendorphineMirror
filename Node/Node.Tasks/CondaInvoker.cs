namespace Node.Tasks;

[AutoRegisteredService(true)]
public class CondaInvoker
{
    public required PowerShellInvoker PowerShellInvoker { get; init; }
    public required CondaManager CondaManager { get; init; }
    public required ILogger<CondaInvoker> Logger { get; init; }

    public Task ExecutePowerShellAtWithCondaEnvAsync(PluginList plugins, PluginType pltype, string script, Action<bool, object>? onRead) =>
        Task.Run(() => ExecutePowerShellAtWithCondaEnv(plugins, pltype, script, onRead));

    public void ExecutePowerShellAtWithCondaEnv(PluginList plugins, PluginType pltype, string script, Action<bool, object>? onRead)
    {
        var plugin = plugins.GetPlugin(pltype);

        var envname = $"{plugin.Type.ToString().ToLowerInvariant()}_{plugin.Version}";
        if (!CondaManager.IsEnvironmentCreated(envname))
        {
            var pluginjson = Path.Combine(Path.GetDirectoryName(plugin.Path)!, "plugin.json");
            if (!File.Exists(pluginjson))
                throw new CondaEnvironmentWasNotCreatedException(envname);

            Logger.LogWarning($"Conda environment {envname} was not found, recreating...");
            var info = JsonConvert.DeserializeObject<SoftwareVersionInfo>(File.ReadAllText(pluginjson)).ThrowIfNull().Installation.ThrowIfNull().Python.ThrowIfNull();
            CondaManager.InitializeEnvironment(plugins.GetPlugin(PluginType.Conda).Path, envname,
                info.Version, info.Conda.Requirements, info.Conda.Channels, info.Pip.Requirements, info.Pip.RequirementFiles, Path.GetFullPath(Path.GetDirectoryName(plugin.Path)!));
        }

        script = $"""
            Set-Location '{Path.GetFullPath(Path.GetDirectoryName(plugin.Path)!)}'
            {CondaManager.GetActivateScript(plugins.GetPlugin(PluginType.Conda).Path, envname)}
            {script}
            """;

        PowerShellInvoker.Invoke(
            script,
            (obj, log) => { log(); onRead?.Invoke(false, obj); },
            (obj, log) => { log(); onRead?.Invoke(true, obj); }
        );
    }
}
