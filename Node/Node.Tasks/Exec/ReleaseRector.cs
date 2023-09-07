namespace Node.Tasks.Exec;

[AutoRegisteredService(true)]
public class ReleaseRector
{
    public const int Version = 1;

    public required Api Api { get; init; }
    public required ImageDetectorLauncher Launcher { get; init; }
    public required ILogger<ReleaseRector> Logger { get; init; }

    public int Errors { get; private set; }
    readonly string Key = File.ReadAllText("rphtaskmgrkey").Trim();

    public async Task<OperationResult<ImmutableArray<ReleaseWithoutRect>>> GetReleasesWithoutRect(CancellationToken token) =>
        await Api.ApiGet<ImmutableArray<ReleaseWithoutRect>>($"{Api.ContentDBEndpoint}/releases/getwithoutrect", "list", "Getting release without rect list",
            Api.SignRequest(Key, ("timestamp", DateTimeOffset.Now.ToUnixTimeMilliseconds().ToString())));

    public async Task<OperationResult<(ReleaseWithoutRect release, string file)[]>> DownloadReleases(IEnumerable<ReleaseWithoutRect> releases, CancellationToken token) =>
        await releases
            .Select(release =>
                from path in DownloadRelease(release, token)
                select (release, path)
            )
            .AggregateParallel(6);

    public async Task<OperationResult<string>> DownloadRelease(ReleaseWithoutRect release, CancellationToken token)
    {
        Logger.LogInformation($"Downloading release {release.Id}");
        var target = Temp.File(release.Id + ".jpg");

        using var stream = await Api.Client.GetStreamAsync(release.Files["release"].Location.Url, token);
        using var file = File.OpenWrite(target);
        await stream.CopyToAsync(file, token);

        return target;
    }

    public async Task<OperationResult> ProcessReleases(IReadOnlyCollection<(ReleaseWithoutRect release, string file)> downloaded, CancellationToken token, bool deleteFiles = true)
    {
        using var _files = Directories.DisposeDelete(deleteFiles ? downloaded.Select(d => d.file).ToArray() : Array.Empty<string>());

        return await downloaded
            .Select(r => ProcessRelease(r.release, r.file, token))
            .Aggregate();
    }
    public async Task<OperationResult> ProcessRelease(ReleaseWithoutRect release, string file, CancellationToken token)
    {
        Logger.LogInformation($"Processing release {release.Id}");
        var rectres = await OperationResult.WrapException(() => Launcher.GenerateRectAsync(file, token));
        if (!rectres)
        {
            Errors++;
            Logger.LogWarning($"{rectres.Error}, replacing rect with 0000. Error count: {Errors}");
            rectres = new ImageDetectorRect(0, 0, 0, 0);
        }

        var rect = rectres.ThrowIfError();
        Logger.LogInformation($"Assigning rect {rect} to release {release.Id}");

        return await Api.ApiPost($"{Api.ContentDBEndpoint}/releases/assignphotorect", $"Assigning rect {rect} to release {release.Id}",
            Api.SignRequest(Key, ("userid", release.UserId), ("iid", release.Iid), ("rect", JsonConvert.SerializeObject(rect, JsonSettings.Lowercase)), ("version", Version.ToStringInvariant())));
    }


    public record ReleaseWithoutRect(string Id, string Iid, string UserId, ImmutableDictionary<string, ReleaseFile> Files);
    public record ReleaseFile(string Filename, long Size, ReleaseStorageLocation Location, string PreviewUrl, string ThumbnailUrl);
    public record ReleaseStorageLocation(string Id, string Server, string Url);
}
