namespace Node.Tasks.IO;

public class TaskInputHandlerByData
{
    readonly ILifetimeScope LifetimeScope;
    readonly ILogger Logger;

    public TaskInputHandlerByData(ILifetimeScope lifetimeScope, ILogger<TaskInputHandlerByData> logger)
    {
        LifetimeScope = lifetimeScope;
        Logger = logger;
    }


    public async Task<object> Download(ITaskInputInfo input, TaskObject obj, CancellationToken token)
    {
        var type = input.Type;
        using var _ = Logger.BeginScope(type);

        var info = LifetimeScope.ResolveKeyed<ITaskInputHandlerInfo>(type);
        return await info.Download(LifetimeScope, input, obj, token);
    }
}
