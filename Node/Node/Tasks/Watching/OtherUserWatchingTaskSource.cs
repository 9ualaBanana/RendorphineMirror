using System.Web;
using Node.Listeners;

namespace Node.Tasks.Watching;

public class OtherUserWatchingTaskHandler : WatchingTaskHandler<OtherUserWatchingTaskInputInfo>
{
    public override WatchingTaskInputType Type => WatchingTaskInputType.OtherNode;

    public OtherUserWatchingTaskHandler(WatchingTask task) : base(task) { }

    public override void StartListening() => Start().Consume();
    async Task Start()
    {
        var node = await Apis.Default.GetNodeAsync(Input.NodeId).ThrowIfError();
        var url = $"http://{node.Info.Ip}:{node.Info.Port}";
        var path = $"dirdiff?sessionid={Settings.SessionId}&path={HttpUtility.UrlEncode(Input.Directory)}";

        StartThreadRepeated(60_000, tick);


        async ValueTask tick()
        {
            var check = await Api.Default.ApiGet<DirectoryDiffListener.DiffOutput>($"{url}/{path}", "value", "Getting directory diff", ("lastcheck", Input.LastCheck.ToString()));
            check.LogIfError();
            if (!check) return;

            var files = check.Value.Files;
            if (files.Length == 0) return;
            files = files.Where(x => x.ModifTime > Input.LastCheck).OrderBy(x => x.ModifTime).ToImmutableArray();

            Task.LogInfo($"Found {files.Length} new files: {string.Join("; ", files)}");

            foreach (var file in files)
            {
                var download = await Api.Default.Download($"{url}/download?sessionid={Settings.SessionId}&path={HttpUtility.UrlEncode(file.Path)}");

                var fsfile = Path.Combine(Task.FSDataDirectory(), Path.GetFileName(file.Path));
                using (var writer = File.OpenWrite(fsfile))
                    await download.CopyToAsync(writer);

                await TaskHandlerList.RegisterTask(Task, fsfile, new TorrentTaskInputInfo(fsfile), new TaskObject(Path.GetFileName(file.Path), file.Size));
                Input.LastCheck = file.ModifTime;
                SaveTask();
            }
        }
    }
}