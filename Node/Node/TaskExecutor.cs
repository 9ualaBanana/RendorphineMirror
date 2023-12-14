using Node.Tasks.Exec.Input;
using Node.Tasks.Exec.Output;
using Node.Tasks.Models.ExecInfo;

namespace Node;

public class TaskExecutor : ITaskExecutor
{
    public required DataDirs Dirs { get; init; }
    public required TaskExecutorByData TaskExecutorByData { get; init; }
    public required IComponentContext Container { get; init; }

    public async Task<QSPreviewOutput> ExecuteQS(IReadOnlyList<string> filesinput, QSPreviewInfo qsinfo, CancellationToken token)
    {
        var input = new TaskFileInput(new ReadOnlyTaskFileList(filesinput.Select(FileWithFormat.FromFile)), ReceivedTask.FSOutputDirectory(Dirs, $"local_{Guid.NewGuid()}"));
        var data = JObject.FromObject(qsinfo).WithProperty("type", TaskAction.GenerateQSPreview.ToString());

        return (QSPreviewOutput) await TaskExecutorByData.Execute(new[] { input }, new[] { data });
    }
}
