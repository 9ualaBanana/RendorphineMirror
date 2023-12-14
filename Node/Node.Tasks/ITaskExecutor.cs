namespace Node.Tasks;

public interface ITaskExecutor
{
    Task<QSPreviewOutput> ExecuteQS(IReadOnlyList<string> Input, QSPreviewInfo Data, CancellationToken token);
}
