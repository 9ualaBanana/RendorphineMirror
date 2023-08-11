using Node.Listeners;

namespace Node.Services.Targets;

public class TaskReceiverTarget : IServiceTarget
{
    public static void CreateRegistrations(ContainerBuilder builder)
    {
        builder.RegisterListener<TaskReceiver>();
        builder.RegisterListener<DirectUploadListener>();
        builder.RegisterListener<DirectDownloadListener>();
    }

    public required TaskExecutorTarget TaskExecutor { get; init; }
    public required TaskReceiver TaskReceiver { get; init; }
    public required DirectUploadListener DirectUploadListener { get; init; }
    public required DirectDownloadListener DirectDownloadListener { get; init; }

    public async Task ExecuteAsync()
    {

    }
}
