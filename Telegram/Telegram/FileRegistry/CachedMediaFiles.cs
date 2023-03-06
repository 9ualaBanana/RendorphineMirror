using Telegram.Bot.Types;
using Telegram.Models;

namespace Telegram.Telegram.FileRegistry;

/// <summary>
/// Enables file processing that spans over multiple <see cref="Update"/> requests.
/// </summary>
public class CachedMediaFiles
{
    readonly Dictionary<string, MediaFile> _cachedMediaFiles = new();
    readonly IWebHostEnvironment _environment;

    public CachedMediaFiles(IWebHostEnvironment environment)
    {
        _environment = environment;
    }

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

    internal string Add(MediaFile mediaFile)
    {
        var index = Guid.NewGuid().ToString();
        _cachedMediaFiles[index] = mediaFile;
        return index;
    }

    internal MediaFile? this[string index]
    { get { _cachedMediaFiles.TryGetValue(index, out var mediaFile); return mediaFile; } }

    internal string PathFor(MediaFile mediaFile, string name)
        => Path.ChangeExtension(Path.Combine(_environment.ContentRootPath, Location, name), mediaFile.Extension);
}
