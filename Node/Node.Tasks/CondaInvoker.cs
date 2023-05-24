namespace Node.Tasks;

public static class CondaInvoker
{
    public static Task ExecutePowerShellAtWithCondaEnvAsync(ITaskExecutionContext context, PluginType pltype, string script, Action<bool, object>? onRead, ILoggable logobj) =>
        Task.Run(() => ExecutePowerShellAtWithCondaEnv(context, pltype, script, onRead, logobj));
    public static void ExecutePowerShellAtWithCondaEnv(ITaskExecutionContext context, PluginType pltype, string script, Action<bool, object>? onRead, ILoggable logobj)
    {
        var condapath = context.GetPlugin(PluginType.Conda).Path;
        var plugin = context.GetPlugin(pltype);

        script = $"""
            Set-Location '{Path.GetFullPath(Path.GetDirectoryName(plugin.Path)!)}'
            {script}
            """;
        script = CondaManager.WrapWithInitEnv(condapath, $"{plugin.Type}_{plugin.Version}", script);

        PowerShellInvoker.Invoke(
            script,
            (obj, log) => { log(); onRead?.Invoke(false, obj); },
            (obj, log) => { log(); onRead?.Invoke(true, obj); },
            logobj
        );
    }
}
