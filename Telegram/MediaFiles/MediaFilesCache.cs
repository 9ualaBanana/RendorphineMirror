using Telegram.Bot.Types;

namespace Telegram.MediaFiles;

/// <summary>
/// Enables file processing that spans over multiple <see cref="Update"/> requests.
/// </summary>
public class MediaFilesCache
{
    internal const int StorageTimeAfterRetrieval = 60_000;

    readonly Dictionary<string, MediaFile> _cachedMediaFiles = new();
    readonly MediaFileDownloader _mediaFileDownloader;
    readonly IWebHostEnvironment _environment;

    public MediaFilesCache(MediaFileDownloader mediaFileDownloader, IWebHostEnvironment environment)
    {
        _mediaFileDownloader = mediaFileDownloader;
        _environment = environment;
    }

    /// <summary>
    /// Path where <see cref="MediaFilesCache"/> is locally stored.
    /// </summary>
    /// <remarks>
    /// Accessing this property creates a corresponding directory if it doesn't exist yet.
    /// </remarks>
    public string Location
    {
        get
        {
            const string Location_ = "cached_media";

            if (!Directory.Exists(Location_))
                Directory.CreateDirectory(Location_);
            return Location_;
        }
    }

    /// <returns>
    /// <see cref="CachedMediaFile"/> physically stored inside <see cref="MediaFilesCache.Location"/>
    /// whose <see cref="CachedMediaFile.Index"/> can be used to <see cref="RetrieveMediaFileWith(string)"/> its.
    /// </returns>
    internal async Task<CachedMediaFile> AddAsync(MediaFile mediaFile, CancellationToken cancellationToken)
    {
        var index = Guid.NewGuid().ToString();
        _cachedMediaFiles[index] = mediaFile;
        string cachedMediaFilePath = RootedPathFor(mediaFile, index);
        await _mediaFileDownloader.UseAsyncToDownload(mediaFile, cachedMediaFilePath, cancellationToken);
        return new CachedMediaFile(mediaFile, index, cachedMediaFilePath);
    }

    /// <remarks>
    /// Physical copy of <see cref="CachedMediaFile"/> stored under the <paramref name="index"/> will be deleted after <see cref="StorageTimeAfterRetrieval"/>.
    /// </remarks>
    internal CachedMediaFile? RetrieveMediaFileWith(string index)
    {
        if (_cachedMediaFiles.TryGetValue(index, out var cachedMediaFile))
        {
            string cachedMediaFilePath = RootedPathFor(cachedMediaFile, index);
            using var removeMediaFileFromCacheAfterwards =
                FuncDispose.Create(async () => { await Task.Delay(StorageTimeAfterRetrieval); System.IO.File.Delete(cachedMediaFilePath); });
            return new(cachedMediaFile, index, cachedMediaFilePath);
        }
        else return null;
    }

    string RootedPathFor(MediaFile mediaFile, string name)
        => Path.ChangeExtension(Path.Combine(_environment.ContentRootPath, Location, name), mediaFile.Extension.ToString());
}
