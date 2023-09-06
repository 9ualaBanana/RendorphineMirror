namespace Node.Tasks.Watching.Handlers.Input;

public class RectReleasesWatchingTaskHandler : WatchingTaskInputHandler<RectReleasesWatchingTaskInputInfo>, ITypedTaskWatchingInput
{
    public static WatchingTaskInputType Type => WatchingTaskInputType.RectReleases;

    const int Version = 1;
    public required PluginList PluginList { get; init; }
    public required HttpClient HttpClient { get; init; }
    public required ILifetimeScope Container { get; init; }

    public override void StartListening()
    {
        var key = File.ReadAllText("rphtaskmgrkey").Trim();
        using var ctx = Container.ResolveForeign<ImageDetectorLauncher>(out var launcher);
        StartThreadRepeated(60_000, run);


        async Task run()
        {
            _ = PluginList.GetPlugin(PluginType.ImageDetector);

            var releases = await Api.Default.ApiGet<ImmutableArray<ReleaseWithoutRect>>($"{Api.ContentDBEndpoint}/releases/getwithoutrect", "list", "Getting release without rect list",
                Api.SignRequest(key, ("timestamp", DateTimeOffset.Now.ToUnixTimeMilliseconds().ToString())))
                .ThrowIfError();

            var downloaded = await releases
                .Select(async r => (r, await download(r)).AsOpResult())
                .AggregateParallel(6)
                .ThrowIfError();

            using var _files = Directories.DisposeDelete(downloaded.Select(d => d.Item2).ToArray());
            foreach (var (release, file) in downloaded)
                await process(release, file, Token.Token);


            async Task<string> download(ReleaseWithoutRect release)
            {
                Logger.LogInformation($"Downloading release {release.Id}");
                Directories.TempFile(out var target, "releaseswithoutrect");

                using var stream = await HttpClient.GetStreamAsync(release.Files["release"].Location.Url, Token.Token);
                using var file = File.OpenWrite(target);
                await stream.CopyToAsync(file, Token.Token);

                return target;
            }
            async Task process(ReleaseWithoutRect release, string file, CancellationToken token)
            {
                Logger.LogInformation($"Processing release {release.Id}");
                var rect = await launcher.GenerateRectAsync(file);

                Logger.LogInformation($"Assigning release {release.Id}");
                await Api.Default.ApiPost($"{Api.ContentDBEndpoint}/releases/assignphotorect", $"Assigning rect {rect} to release {release.Id}",
                    Api.SignRequest(key, ("userid", release.UserId), ("iid", release.Iid), ("rect", JsonConvert.SerializeObject(rect, JsonSettings.Lowercase)), ("version", Version.ToStringInvariant())))
                    .ThrowIfError();
            }
        }
    }


    record ReleaseWithoutRect(string Id, string Iid, string UserId, ImmutableDictionary<string, ReleaseFile> Files);
    record ReleaseFile(string Filename, long Size, ReleaseStorageLocation Location, string PreviewUrl, string ThumbnailUrl);
    record ReleaseStorageLocation(string Id, string Server, string Url);
}
