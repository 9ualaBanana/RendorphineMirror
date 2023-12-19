using Node.Tasks.Exec.Input;
using Node.Tasks.Exec.Output;
using Node.Tasks.Models.ExecInfo;

namespace Node;

public class TaskExecutor : ITaskExecutor
{
    public required DataDirs Dirs { get; init; }
    public required ILifetimeScope Container { get; init; }

    public async Task<QSPreviewOutput> ExecuteQS(IReadOnlyList<string> filesinput, QSPreviewInfo qsinfo, CancellationToken token)
    {
        var input = new TaskFileInput(new ReadOnlyTaskFileList(filesinput.Select(FileWithFormat.FromFile)), ReceivedTask.FSOutputDirectory(Dirs, $"local_{Guid.NewGuid()}"));
        var data = JObject.FromObject(qsinfo).WithProperty("type", TaskAction.GenerateQSPreview.ToString());

        return (QSPreviewOutput) await Execute(input, data, token);
    }

    public async Task<object> Execute(object input, JObject data, CancellationToken token)
    {
        using var scope = Container.BeginLifetimeScope(builder =>
        {
            builder.RegisterType<Node.Tasks.Exec.TaskExecutor>()
                .SingleInstance();

            builder.RegisterType<TaskExecutorByData>()
                .SingleInstance();

            builder.RegisterType<TaskOutputDirectoryProvider>()
                .As<ITaskOutputDirectoryProvider>()
                .SingleInstance();

            builder.RegisterType<TaskProgressSetter>()
                .As<ITaskProgressSetter>()
                .SingleInstance();

            builder.RegisterDecorator<ITaskProgressSetter>((ctx, parameters, instance) => new ThrottledProgressSetter(TimeSpan.FromSeconds(5), instance));
        });

        return await scope.Resolve<TaskExecutorByData>().Execute([input], [data]);
    }


    class TaskProgressSetter : ITaskProgressSetter
    {
        public required ILogger<TaskProgressSetter> Logger { get; init; }

        public void Set(double progress) => Logger.Trace($"Task progress: {progress}");
    }
}
