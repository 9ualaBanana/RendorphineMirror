using Newtonsoft.Json;

namespace Node.Tasks.Watching;

public class LocalWatchingTaskSource : IWatchingTaskSource
{
    public WatchingTaskInputOutputType Type => WatchingTaskInputOutputType.Local;

    [LocalDirectory] public readonly string Directory;
    [JsonIgnore] FileSystemWatcher? Watcher;

    readonly List<string> SavedFiles = new();

    public LocalWatchingTaskSource(string directory) => Directory = directory;

    public void StartListening(WatchingTask task)
    {
        // TODO: not use watcher sincle it doesnt work when node is stopped
        Watcher?.Dispose();

        System.IO.Directory.CreateDirectory(Directory);
        Watcher = new FileSystemWatcher(Directory) { IncludeSubdirectories = true };
        Watcher.Created += async (obj, e) =>
        {
            if (!File.Exists(e.FullPath)) return;

            var filename = Path.GetFileName(e.FullPath);
            var info = new TorrentTaskInputInfo(e.FullPath, link: null!);
            await task.RegisterTask(filename, info);
        };

        Watcher.EnableRaisingEvents = true;
    }

    public void Dispose() => Watcher?.Dispose();
}
