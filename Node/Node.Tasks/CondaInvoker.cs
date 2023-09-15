namespace Node.Tasks;

public static class CondaInvoker
{
    public static Task ExecutePowerShellAtWithCondaEnvAsync(PluginList plugins, PluginType pltype, string script, Action<bool, object>? onRead, ILogger logger) =>
        Task.Run(() => ExecutePowerShellAtWithCondaEnv(plugins, pltype, script, onRead, logger));
    public static void ExecutePowerShellAtWithCondaEnv(PluginList plugins, PluginType pltype, string script, Action<bool, object>? onRead, ILogger logger)
    {
        var plugin = plugins.GetPlugin(pltype);

        var envname = $"{plugin.Type.ToString().ToLowerInvariant()}_{plugin.Version}";
        if (!CondaManager.IsEnvironmentCreated(envname))
            throw new CondaEnvironmentWasNotCreatedException(envname);

        script = $"""
            Set-Location '{Path.GetFullPath(Path.GetDirectoryName(plugin.Path)!)}'
            {CondaManager.GetRunInEnvironmentScript(plugins.GetPlugin(PluginType.Conda).Path, envname, script)}
            """;

        PowerShellInvoker.Invoke(
            script,
            (obj, log) => { log(); onRead?.Invoke(false, obj); },
            (obj, log) => { log(); onRead?.Invoke(true, obj); },
            logger
        );
    }
}
