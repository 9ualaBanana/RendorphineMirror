namespace Node.Tasks;

[AutoRegisteredService(true)]
public class CondaInvoker
{
    public required PowerShellInvoker PowerShellInvoker { get; init; }
    public required CondaManager CondaManager { get; init; }

    public Task ExecutePowerShellAtWithCondaEnvAsync(PluginList plugins, PluginType pltype, string script, Action<bool, object>? onRead) =>
        Task.Run(() => ExecutePowerShellAtWithCondaEnv(plugins, pltype, script, onRead));

    public void ExecutePowerShellAtWithCondaEnv(PluginList plugins, PluginType pltype, string script, Action<bool, object>? onRead)
    {
        var plugin = plugins.GetPlugin(pltype);

        var envname = $"{plugin.Type.ToString().ToLowerInvariant()}_{plugin.Version}";
        if (!CondaManager.IsEnvironmentCreated(envname))
            throw new CondaEnvironmentWasNotCreatedException(envname);

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
