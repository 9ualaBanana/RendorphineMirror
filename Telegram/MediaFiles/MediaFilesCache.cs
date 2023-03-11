using System.Collections.Specialized;
using Telegram.Bot.Types;
using File = System.IO.File;

namespace Telegram.MediaFiles;

/// <summary>
/// Enables file processing that spans over multiple <see cref="Update"/> requests.
/// </summary>
public class MediaFilesCache
{
    internal const int CachingTime = 300_000;

    readonly AutoStorage<CachedMediaFile> _cachedMediaFiles;
    readonly MediaFileDownloader _mediaFileDownloader;
    readonly IWebHostEnvironment _environment;

    readonly ILogger<MediaFilesCache> _logger;

    public MediaFilesCache(MediaFileDownloader mediaFileDownloader, IWebHostEnvironment environment, ILogger<MediaFilesCache> logger)
    {
        _cachedMediaFiles = new(new CachedMediaFile.IndexEqualityComparer(), (StorageTime)CachingTime);
        _mediaFileDownloader = mediaFileDownloader;
        _environment = environment;
        _location = Rooted("cached_media");
        _logger = logger;

        _cachedMediaFiles.ItemStorageTimeElapsed += (_, expiredMediaFile) =>
        {
            File.Delete(expiredMediaFile.Value.Path);
            _logger.LogTrace($"Media file with index {expiredMediaFile.Value.Index} has expired");
        };
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
            if (!Directory.Exists(_location))
                Directory.CreateDirectory(_location);
            return _location;
        }
    }
    readonly string _location;

    internal async Task<CachedMediaFile> AddAsync(MediaFile mediaFile, CancellationToken cancellationToken)
        => await CacheAsync(mediaFile, StorageTime.Default, cancellationToken);

    /// <returns>
    /// <see cref="CachedMediaFile"/> physically stored inside <see cref="MediaFilesCache.Location"/>
    /// whose <see cref="CachedMediaFile.Index"/> can be used to <see cref="TryRetrieveMediaFileWith(string)"/> its.
    /// </returns>
    internal async Task<CachedMediaFile> CacheAsync(MediaFile mediaFile, StorageTime cachingTime, CancellationToken cancellationToken)
    {
        var index = Guid.NewGuid();
        string cachedMediaFilePath = RootedPathFor(mediaFile, index.ToString());
        await _mediaFileDownloader.UseAsyncToDownload(mediaFile, cachedMediaFilePath, cancellationToken);
        var cachedMediaFile = new CachedMediaFile(mediaFile, index, cachedMediaFilePath);
        _cachedMediaFiles.Add(cachedMediaFile, cachingTime);
        return cachedMediaFile;
    }

    /// <remarks>
    /// Physical copy of <see cref="CachedMediaFile"/> stored under the <paramref name="index"/> will be deleted after <see cref="CachingTime"/>.
    /// </remarks>
    internal CachedMediaFile? TryRetrieveMediaFileWith(Guid index)
    {
        if (_cachedMediaFiles.TryGetValue(CachedMediaFile.WithIndex(index), out var cachedMediaFile))
            return cachedMediaFile;
        else return null;
    }

    string RootedPathFor(MediaFile mediaFile, string name)
        => Path.ChangeExtension(Rooted(Location, name), mediaFile.Extension.ToString());

    string Rooted(params string[] paths)
        => Path.Combine(paths.Prepend(_environment.ContentRootPath).ToArray());
}
