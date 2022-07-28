using Newtonsoft.Json;

namespace Node.Tasks.Repeating;

public class LocalRepeatingTaskSource : IRepeatingTaskSource
{
    public event Action<RepeatingTaskFileAddedEventArgs>? FileAdded;

    [LocalFile] public readonly string Directory;
    [JsonIgnore] FileSystemWatcher? Watcher;

    readonly List<string> SavedFiles = new();

    public LocalRepeatingTaskSource(string directory) => Directory = directory;

    public void StartListening()
    {
        Watcher?.Dispose();

        System.IO.Directory.CreateDirectory(Directory);
        Watcher = new FileSystemWatcher(Directory) { IncludeSubdirectories = true };
        Watcher.Created += (obj, e) =>
        {
            if (!File.Exists(e.FullPath)) return;

            var filename = Path.GetFileName(e.FullPath);
            var info = new UserTaskInputInfo(e.FullPath);
            FileAdded?.Invoke(new(filename, info));
        };

        Watcher.EnableRaisingEvents = true;
    }

    public void Dispose() => Watcher?.Dispose();
}
