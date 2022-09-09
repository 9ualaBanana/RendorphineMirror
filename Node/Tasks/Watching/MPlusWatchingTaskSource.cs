using Newtonsoft.Json;

namespace Node.Tasks.Watching;

public class MPlusWatchingTaskSource : IWatchingTaskSource
{
    public WatchingTaskInputOutputType Type => WatchingTaskInputOutputType.MPlus;

    [JsonIgnore] readonly CancellationTokenSource TokenSource = new();

    [MPlusDirectory] public readonly string Directory;
    [DescriberIgnore] public string SinceIid { get; private set; }

    public MPlusWatchingTaskSource(string directory) : this(directory, string.Empty) { }

    [JsonConstructor]
    public MPlusWatchingTaskSource(string directory, string sinceIid)
    {
        Directory = directory;
        SinceIid = sinceIid;
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

                    await start();
                    await Task.Delay(60_000);


                    async Task start()
                    {
                        var res = await Api.ApiGet<ImmutableArray<NewItem>>($"{Api.TaskManagerEndpoint}/getmynewitems", "items", "Getting new items", ("sessionid", Settings.SessionId!), ("sinceiid", SinceIid), ("directory", Directory));
                        res.LogIfError();
                        if (!res) return;

                        var items = res.Value;
                        if (items.Length == 0) return;

                        foreach (var item in items)
                        {
                            var input = new MPlusTaskInputInfo(item.Iid);
                            task.LogInfo($"New file found: {item.Iid} {Path.ChangeExtension(item.Files.Jpeg.FileName, null)}");

                            await task.RegisterTask(item.Files.Jpeg.FileName, input);
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


    record NewItem(string Iid, NewItemFiles Files);
    record NewItemFiles(NewItemFile Jpeg, NewItemFile Mov);
    record NewItemFile(string FileName, long Size);
}
