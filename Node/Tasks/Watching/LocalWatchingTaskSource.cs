using Newtonsoft.Json;

namespace Node.Tasks.Watching;

public class LocalWatchingTaskSource : IWatchingTaskSource
{
    public WatchingTaskInputOutputType Type => WatchingTaskInputOutputType.Local;

    [JsonIgnore] readonly CancellationTokenSource TokenSource = new();

    [LocalDirectory] public readonly string Directory;
    long LastCheck = 0;

    public LocalWatchingTaskSource(string directory) => Directory = directory;

    public void StartListening(WatchingTask task)
    {
        new Thread(async () =>
        {
            while (true)
            {
                try
                {
                    if (TokenSource.IsCancellationRequested) return;

                    await Task.Delay(60_000);
                    if (task.IsPaused) continue;


                    var files = System.IO.Directory.GetFiles(Directory, "*", SearchOption.AllDirectories)
                        .Where(x => new DateTimeOffset(File.GetCreationTimeUtc(x)).ToUnixTimeMilliseconds() > LastCheck);

                    foreach (var file in files)
                        await start(file);
                }
                catch (Exception ex) { task.LogErr(ex); }
            }
        })
        { IsBackground = true }.Start();


        async ValueTask start(string file)
        {
            if (!File.Exists(file)) return;

            var filename = Path.GetFileName(file);
            var info = new TorrentTaskInputInfo(file, link: null!);
            await task.RegisterTask(filename, info);

            LastCheck = new DateTimeOffset(File.GetCreationTimeUtc(file)).ToUnixTimeMilliseconds();
            NodeSettings.WatchingTasks.Save(task);
        }
    }

    public void Dispose() => TokenSource.Dispose();
}
