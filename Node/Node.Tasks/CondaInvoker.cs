namespace Node.Tasks;

public static class CondaInvoker
{
    public static Task ExecutePowerShellAtWithCondaEnvAsync(ITaskExecutionContext context, PluginType pltype, string script, Action<bool, object>? onRead, ILoggable logobj) =>
        Task.Run(() => ExecutePowerShellAtWithCondaEnv(context, pltype, script, onRead, logobj));
    public static void ExecutePowerShellAtWithCondaEnv(ITaskExecutionContext context, PluginType pltype, string script, Action<bool, object>? onRead, ILoggable logobj)
    {
        var plugin = context.GetPlugin(pltype);

        var envname = $"{plugin.Type}_{plugin.Version}";
        if (!CondaManager.IsEnvironmentCreated(envname))
            throw new Exception($"Conda environment {envname} was not created");

        script = $"""
            Set-Location '{Path.GetFullPath(Path.GetDirectoryName(plugin.Path)!)}'
            {CondaManager.GetRunInEnvironmentScript(context.GetPlugin(PluginType.Conda).Path, envname, script)}
            """;

        PowerShellInvoker.Invoke(
            script,
            (obj, log) => { log(); onRead?.Invoke(false, obj); },
            (obj, log) => { log(); onRead?.Invoke(true, obj); },
            logobj
        );
    }
}
