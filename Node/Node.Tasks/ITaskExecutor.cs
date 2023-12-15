namespace Node.Tasks;

public interface ITaskExecutor
{
    Task<QSPreviewOutput> ExecuteQS(IReadOnlyList<string> filesinput, QSPreviewInfo qsinfo, CancellationToken token);
}
