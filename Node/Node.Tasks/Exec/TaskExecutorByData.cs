namespace Node.Tasks.Exec;

public class TaskExecutorByData
{
    readonly ILifetimeScope LifetimeScope;
    readonly ITaskProgressSetter ProgressSetter;
    readonly ILogger Logger;

    public TaskExecutorByData(ILifetimeScope lifetimeScope, ITaskProgressSetter progressSetter, ILogger<TaskExecutorByData> logger)
    {
        LifetimeScope = lifetimeScope;
        ProgressSetter = progressSetter;
        Logger = logger;
    }


    public async Task<IReadOnlyList<object>> Execute(object firstinput, IReadOnlyList<JObject> datas)
    {
        var results = new[] { firstinput };

        var index = 0;
        foreach (var data in datas)
        {
            using var scope = LifetimeScope.BeginLifetimeScope(builder =>
            {
                builder.RegisterDecorator<ITaskProgressSetter>((ctx, parameters, instance) => new ProgressSetterSubtaskOverlay(index, datas.Count, instance));
            });

            var inputs = results.SelectMany(result => result switch
            {
                IConvertibleToInput convertible => new[] { convertible.ConvertToInput(index, GetTaskName(data)) },
                IConvertibleToMultiInput multiconvertible => multiconvertible.ConvertToInput(index, GetTaskName(data)),
                _ => new[] { result },
            });

            results = await Task.WhenAll(inputs.Select(async input => await ExecuteSingle(scope, input, data, index.ToStringInvariant())));
            index++;
        }

        ProgressSetter.Set(1);
        return results;
    }
    async Task<object> ExecuteSingle(ILifetimeScope container, object input, JObject data, string? loginfo = null)
    {
        var tasktype = GetTaskName(data);
        using var _ = Logger.BeginScope($"{tasktype}{(loginfo is null ? null : $" {loginfo}")}");

        var info = container.ResolveKeyed<IPluginActionInfo>(tasktype);
        return await info.Execute(container, input, data);
    }

    public static TaskAction GetTaskName(JObject data)
    {
        var tasktypename = data.Property("type", StringComparison.OrdinalIgnoreCase)?.Value.Value<string>()
            ?? throw new InvalidOperationException($"No type in task data {data}");

        if (!Enum.TryParse<TaskAction>(tasktypename, out var tasktype))
            throw new InvalidOperationException($"Invalid task data type {tasktypename}");

        return tasktype;
    }


    record ProgressSetterSubtaskOverlay(int Subtask, int MaxSubtasks, ITaskProgressSetter Setter) : ITaskProgressSetter
    {
        public void Set(double progress)
        {
            var subtaskpart = 1d / MaxSubtasks;
            Setter.Set((progress * subtaskpart) + (subtaskpart * Subtask));
        }
    }
}
