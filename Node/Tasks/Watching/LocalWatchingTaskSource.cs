using Newtonsoft.Json;

namespace Node.Tasks.Watching;

public class LocalWatchingTaskSource : IWatchingTaskSource
{
    public WatchingTaskInputOutputType Type => WatchingTaskInputOutputType.Local;

    [LocalDirectory] public readonly string Directory;
    [JsonIgnore] FileSystemWatcher? Watcher;
    long LastCheck = 0;

    public LocalWatchingTaskSource(string directory) => Directory = directory;

    public void StartListening(WatchingTask task)
    {
        Watcher?.Dispose();

        System.IO.Directory.CreateDirectory(Directory);
        Watcher = new FileSystemWatcher(Directory) { IncludeSubdirectories = true };
        Watcher.Created += async (obj, e) => await start(e.FullPath);

        Watcher.EnableRaisingEvents = true;

        Task.Run(async () =>
        {
            var files = System.IO.Directory.GetFiles(Directory, "*", SearchOption.AllDirectories)
                .Where(x => new DateTimeOffset(File.GetCreationTimeUtc(x)).ToUnixTimeMilliseconds() > LastCheck);

            foreach (var file in files)
                await start(file);
        }).Consume();


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

    public void Dispose() => Watcher?.Dispose();
}
