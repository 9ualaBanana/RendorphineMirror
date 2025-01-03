namespace Node.Tasks.Watching;

public class LocalWatchingTaskHandler : WatchingTaskHandler<LocalWatchingTaskInputInfo>
{
    public override WatchingTaskInputType Type => WatchingTaskInputType.Local;

    public LocalWatchingTaskHandler(WatchingTask task) : base(task) { }

    public override void StartListening()
    {
        StartThreadRepeated(60_000, tick);


        async ValueTask tick()
        {
            var files = System.IO.Directory.GetFiles(Input.Directory, "*", SearchOption.AllDirectories)
                .Where(x => new DateTimeOffset(File.GetCreationTimeUtc(x)).ToUnixTimeMilliseconds() > Input.LastCheck);

            foreach (var file in files)
                await start(file);
        }
        async ValueTask start(string file)
        {
            if (!File.Exists(file)) return;

            var filename = Path.GetFileName(file);
            var info = new TorrentTaskInputInfo(file, link: null!);
            await Task.RegisterTask(filename, info);

            Input.LastCheck = new DateTimeOffset(File.GetCreationTimeUtc(file)).ToUnixTimeMilliseconds();
            SaveTask();
        }
    }
}