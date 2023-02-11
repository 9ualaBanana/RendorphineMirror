using Telegram.Bot.Types;
using Telegram.Telegram.Updates.Images.Models;

namespace Telegram.Telegram.FileRegistry;

/// <summary>
/// Enables file processing that spans over multiple <see cref="Update"/> requests.
/// </summary>
public class CachedFiles
{
    readonly Dictionary<string, TelegramMediaFile> _cachedFiles = new();

    public string Location
    {
        get
        {
            if (!Directory.Exists(_Location)) Directory.CreateDirectory(_Location);
            return _Location;
        }
    }
    const string _Location = "cached_files";

    internal string Add(TelegramMediaFile mediaFile)
    {
        var key = Guid.NewGuid().ToString();
        _cachedFiles[key] = mediaFile;
        return key;
    }

    internal TelegramMediaFile? this[string key]
    { get { _cachedFiles.TryGetValue(key, out var file); return file; } }
}
