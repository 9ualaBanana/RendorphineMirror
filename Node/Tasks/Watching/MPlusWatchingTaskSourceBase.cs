using System.Text.Json.Serialization;

namespace Node.Tasks.Watching;

public abstract class MPlusWatchingTaskSourceBase : IWatchingTaskSource
{
    public abstract WatchingTaskInputOutputType Type { get; }

    [JsonIgnore] readonly CancellationTokenSource TokenSource = new();

    public string? SinceIid;

    protected MPlusWatchingTaskSourceBase(string? sinceIid) => SinceIid = sinceIid;

    protected abstract ValueTask<OperationResult<ImmutableArray<MPlusNewItem>>> FetchItemsAsync();

    public void StartListening(WatchingTask task)
    {
        new Thread(async () =>
        {
            while (true)
            {
                try
                {
                    if (TokenSource.IsCancellationRequested) return;

                    await start();
                    await Task.Delay(60_000);


                    async Task start()
                    {
                        var res = await FetchItemsAsync();
                        res.LogIfError();
                        if (!res) return;

                        var items = res.Value;
                        if (items.Length == 0) return;

                        foreach (var item in items.OrderBy<MPlusNewItem, long>(x => x.Registered))
                        {
                            var fileName = item.Files.Jpeg.FileName;
                            var input = new MPlusTaskInputInfo(item.Iid, item.UserId);
                            task.LogInfo($"New file found: {item.Iid} {Path.ChangeExtension(fileName, null)}");

                            var output =
                                (task.Output as IMPlusWatchingTaskOutputInfo)?.CreateOutput(item, fileName)
                                ?? task.Output.CreateOutput(fileName);

                            await task.RegisterTask(input, output);

                            SinceIid = item.Iid;
                            NodeSettings.WatchingTasks.Save();
                        }
                    }
                }
                catch (Exception ex) { task.LogErr(ex); }
            }
        })
        { IsBackground = true }.Start();
    }

    public void Dispose() => TokenSource.Cancel();
}
