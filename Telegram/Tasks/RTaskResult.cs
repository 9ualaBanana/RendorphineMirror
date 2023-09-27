namespace Telegram.Tasks.ResultPreview;

internal abstract partial class RTaskResult
{
    /// <summary>
    /// ID of the task that was responsible for producing this <see cref="RTaskResult"/>.
    /// </summary>
    internal string Id { get; }

    internal TaskAction Action { get; }

    /// <summary>
    /// Name of the node that was responsible for producing this <see cref="RTaskResult"/>.
    /// </summary>
    internal string Executor { get; }

    protected RTaskResult(ExecutedRTask executedRTask)
    {
        Id = executedRTask.Id;
        Action = executedRTask.Action;
        Executor = executedRTask.Executor;
    }
}
