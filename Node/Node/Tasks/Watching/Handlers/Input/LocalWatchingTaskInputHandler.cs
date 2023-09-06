namespace Node.Tasks.Watching.Handlers.Input;

public class LocalWatchingTaskInputHandler : WatchingTaskInputHandler<LocalWatchingTaskInputInfo>, ITypedTaskWatchingInput
{
    public static WatchingTaskInputType Type => WatchingTaskInputType.Local;

    public override void StartListening()
    {
        StartThreadRepeated(60_000, tick);


        async Task tick()
        {
            var files = System.IO.Directory.GetFiles(Input.Directory, "*", SearchOption.AllDirectories)
                .Where(x => new DateTimeOffset(File.GetCreationTimeUtc(x)).ToUnixTimeMilliseconds() > Input.LastCheck);

            foreach (var file in files)
                await start(file);
        }
        async Task start(string file)
        {
            if (!File.Exists(file)) return;

            var filename = Path.GetFileName(file);
            var info = new TorrentTaskInputInfo(file, link: null!);
            await TaskRegistration.RegisterAsync(Task, filename, info, new TaskObject(filename, new FileInfo(file).Length));

            Input.LastCheck = new DateTimeOffset(File.GetCreationTimeUtc(file)).ToUnixTimeMilliseconds();
            SaveTask();
        }
    }
}
