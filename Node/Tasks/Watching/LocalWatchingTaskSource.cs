using Newtonsoft.Json;

namespace Node.Tasks.Watching;

public class LocalWatchingTaskSource : IWatchingTaskSource
{
    public event Action<WatchingTaskFileAddedEventArgs>? FileAdded;
    public WatchingTaskInputOutputType Type => WatchingTaskInputOutputType.Local;

    [LocalDirectory] public readonly string Directory;
    [JsonIgnore] FileSystemWatcher? Watcher;

    readonly List<string> SavedFiles = new();

    public LocalWatchingTaskSource(string directory) => Directory = directory;

    public void StartListening(WatchingTask task)
    {
        Watcher?.Dispose();

        System.IO.Directory.CreateDirectory(Directory);
        Watcher = new FileSystemWatcher(Directory) { IncludeSubdirectories = true };
        Watcher.Created += (obj, e) =>
        {
            if (!File.Exists(e.FullPath)) return;

            var filename = Path.GetFileName(e.FullPath);
            var info = new TorrentTaskInputInfo(e.FullPath, link: null!);
            FileAdded?.Invoke(new(filename, info));
        };

        Watcher.EnableRaisingEvents = true;
    }

    public void Dispose() => Watcher?.Dispose();
}
