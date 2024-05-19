using System.Web;
using Node.Listeners;

namespace Node.Tasks.Watching.Handlers.Input;

public class OtherUserWatchingTaskHandler : WatchingTaskInputHandler<OtherUserWatchingTaskInputInfo>, ITypedTaskWatchingInput
{
    public static WatchingTaskInputType Type => WatchingTaskInputType.OtherNode;

    public required Apis Api { get; init; }
    public required DataDirs Dirs { get; init; }

    public override void StartListening() => Start().Consume();

    async Task Start()
    {
        var node = await Api.GetNodeAsync(Input.NodeId).ThrowIfError();
        var url = $"http://{node.Info.Ip}:{node.Info.Port}";
        var path = $"dirdiff?sessionid={Settings.SessionId}&path={HttpUtility.UrlEncode(Input.Directory)}";

        StartThreadRepeated(60_000, tick);


        async Task tick()
        {
            var check = await Api.Api.ApiGet<DirectoryDiffController.DiffOutput>($"{url}/{path}", "value", "Getting directory diff", ("lastcheck", Input.LastCheck.ToString()));
            check.LogIfError();
            if (!check) return;

            var files = check.Value.Files;
            if (files.Length == 0) return;
            files = files.Where(x => x.ModifTime > Input.LastCheck).OrderBy(x => x.ModifTime).ToImmutableArray();

            Logger.LogInformation($"Found {files.Length} new files: {string.Join("; ", files)}");

            foreach (var file in files)
            {
                var download = await Api.Api.Client.GetStreamAsync($"{url}/download?sessionid={Settings.SessionId}&path={HttpUtility.UrlEncode(file.Path)}");

                var fsfile = Path.Combine(Task.FSDataDirectory(Dirs), Path.GetFileName(file.Path));
                using (var writer = File.OpenWrite(fsfile))
                    await download.CopyToAsync(writer);

                await TaskRegistration.RegisterAsync(Task, fsfile, new TorrentTaskInputInfo(fsfile), new TaskObject(Path.GetFileName(file.Path), file.Size));
                Input.LastCheck = file.ModifTime;
                SaveTask();
            }
        }
    }
}
