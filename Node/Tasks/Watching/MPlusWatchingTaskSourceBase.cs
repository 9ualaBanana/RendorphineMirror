using System.Text.Json.Serialization;

namespace Node.Tasks.Watching;

public abstract class MPlusWatchingTaskSourceBase : IWatchingTaskSource
{
    public abstract WatchingTaskInputOutputType Type { get; }

    [JsonIgnore] readonly CancellationTokenSource TokenSource = new();

    [Default(false)] public readonly bool SkipWatermarked;
    public string? SinceIid;

    protected MPlusWatchingTaskSourceBase(string? sinceIid, bool skipWatermarked = false)
    {
        SinceIid = sinceIid;
        SkipWatermarked = skipWatermarked;
    }

    protected abstract ValueTask<OperationResult<ImmutableArray<MPlusNewItem>>> FetchItemsAsync();

    protected virtual async Task Tick(WatchingTask task)
    {
        var res = await FetchItemsAsync();
        res.LogIfError();
        if (!res) return;

        var items = res.Value;
        if (items.Length == 0) return;

        foreach (var item in items.OrderBy<MPlusNewItem, long>(x => x.Registered))
        {
            var fileName = item.Files.Jpeg.FileName;

            if (SkipWatermarked && isWatermarked())
            {
                task.LogInfo($"File {item.Iid} {Path.ChangeExtension(fileName, null)} is already watermarked, skipping");
                continue;
            }

            task.LogInfo($"Adding new file {item.Iid} {Path.ChangeExtension(fileName, null)}");

            var output =
                (task.Output as IMPlusWatchingTaskOutputInfo)?.CreateOutput(item, fileName)
                ?? task.Output.CreateOutput(fileName);

            var input = new MPlusTaskInputInfo(item.Iid, item.UserId);
            await task.RegisterTask(input, output);

            SinceIid = item.Iid;
            NodeSettings.WatchingTasks.Save(task);


            bool isWatermarked()
            {
                if (item.QSPreview is null) return false;
                if (item.Files.Mov is not null && item.QSPreview.Mp4 is null) return false;

                return true;
            }
        }
    }
    public void StartListening(WatchingTask task)
    {
        new Thread(async () =>
        {
            while (true)
            {
                try
                {
                    if (TokenSource.IsCancellationRequested) return;

                    await Tick(task);
                    await Task.Delay(60_000);
                }
                catch (Exception ex) { task.LogErr(ex); }
            }
        })
        { IsBackground = true }.Start();
    }

    public void Dispose() => TokenSource.Cancel();
}
