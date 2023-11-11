using MonoTorrent;

namespace Node.Tasks.IO.Handlers.Output;

public static class Torrent
{
    public class UploadHandler : FileTaskUploadHandler<TorrentTaskOutputInfo>, ITypedTaskOutput
    {
        public static TaskOutputType Type => TaskOutputType.Torrent;
        public required IRegisteredTaskApi ApiTask { get; init; }
        public required Apis Api { get; init; }
        public required IQueuedTasksStorage QueuedTasks { get; init; }
        public required TorrentClient TorrentClient { get; init; }
        public required ITaskOutputDirectoryProvider ResultDirectoryProvider { get; init; }
        public required NodeSettingsInstance Settings { get; init; }

        protected override async Task UploadResultImpl(TorrentTaskOutputInfo info, ITaskInputInfo input, ReadOnlyTaskFileList result, CancellationToken token)
        {
            // TODO: fix uploading FSOutputDirectory instead of outputfiles

            var prefix = GetCommonDirectory(result.Paths);
            if (ApiTask.IsFromSameNode(Settings))
            {
                foreach (var file in result.Paths)
                    File.Copy(file, Path.Combine(ResultDirectoryProvider.OutputDirectory, Path.GetRelativePath(prefix, file)));

                return;
            }

            var (_, manager) = await TorrentClient.CreateAddTorrent(await TorrentClient.CreateTorrent(new TorrentFileSource("result", result.Select(f => new FileMapping(f.Path, Path.GetRelativePath(prefix, f.Path))))), prefix);
            await TorrentClient.AddTrackers(manager, true);
            Logger.LogInformation($"Result magnet uri: {manager.MagnetLink.ToV1String()}");

            var args = new[]
            {
                ("taskid", ApiTask.Id),
                ("link", manager.MagnetLink.ToV1String()),
                ("key", (input as MPlusTaskInputInfo)?.Iid ?? Guid.NewGuid().ToString()),
            };

            var post = await Api.ShardGet(ApiTask, "inittorrenttaskoutput", "Updating output magnet uri", args);
            post.ThrowIfError();

            // info.Link = manager.MagnetLink.ToV1String();
            QueuedTasks.QueuedTasks.Save((ReceivedTask) ApiTask);

            Logger.LogInformation($"Waiting for torrent result upload ({manager.InfoHash.ToHex()})");
            while (true)
            {
                await Task.Delay(30_000, token);

                var state = await Api.GetTaskStateAsync(ApiTask).ThrowIfError();
                if (state is null || state.State.IsFinished()) return;
            }
        }

        static string GetCommonDirectory(IEnumerable<string> strings)
        {
            return strings
                .SelectMany(s => getdirs(s).ToArray())
                .GroupBy(d => d)
                .Where(g => g.Count() != 1)
                .MaxBy(g => g.Count())!
                .Key;


            static IEnumerable<string> getdirs([DisallowNull] string? path)
            {
                while (true)
                {
                    path = Path.GetDirectoryName(path);
                    if (path is null) yield break;

                    yield return path;
                }
            }
        }

        class TorrentFileSource : ITorrentFileSource
        {
            public string TorrentName { get; }
            public IEnumerable<FileMapping> Files { get; }

            public TorrentFileSource(string torrentName, IEnumerable<FileMapping> files)
            {
                TorrentName = torrentName;
                Files = files;
            }
        }
    }
    public class CompletionChecker : TaskCompletionChecker<TorrentTaskOutputInfo>, ITypedTaskOutput
    {
        public static TaskOutputType Type => TaskOutputType.Torrent;

        public override bool CheckCompletion(TorrentTaskOutputInfo info, TaskState state) =>
            info.Data?.Values.All(data => data.Link is not null) == true;
    }
    public class CompletionHandler : TaskCompletionHandler<TorrentTaskOutputInfo>, ITypedTaskOutput
    {
        public static TaskOutputType Type => TaskOutputType.Torrent;
        public required IRegisteredTaskApi ApiTask { get; init; }
        public required TorrentClient TorrentClient { get; init; }
        public required ITaskOutputDirectoryProvider ResultDirectoryProvider { get; init; }
        public required NodeSettingsInstance Settings { get; init; }

        public override async Task OnPlacedTaskCompleted(TorrentTaskOutputInfo info)
        {
            // if task is local, downloading already handled by UploadResult
            if (ApiTask.IsFromSameNode(Settings)) return;

            await Task.WhenAll(info.Data.ThrowIfNull().Values.Select(async data =>
            {
                Logger.LogInformation($"Downloading result from torrent {data}");

                var manager = await TorrentClient.StartMagnet(data.Link.ThrowIfNull(), ResultDirectoryProvider.OutputDirectory);
                await TorrentClient.AddTrackers(manager, true);
                await TorrentClient.WaitForCompletion(manager, TimeSpan.FromMinutes(5));
            }));
        }
    }
}
