using Node.Listeners;

namespace Node.Services.Targets;

public class TaskReceiverTarget : IServiceTarget
{
    public static void CreateRegistrations(ContainerBuilder builder) { }

    public required TaskExecutorTarget TaskExecutor { get; init; }
    public required TaskReceiver TaskReceiver { get; init; }
    public required DirectUploadListener DirectUploadListener { get; init; }
    public required DirectDownloadListener DirectDownloadListener { get; init; }
}
