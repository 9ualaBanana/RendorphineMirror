using Telegram.Telegram.Updates.Images.Models;

namespace Telegram.Telegram.FileRegistry;

// Delete all already downloaded files from their folder upon initialization.
// Also delete file from server when it expires.
public class TelegramFileRegistry
{
    public string Path
    {
        get
        {
            if (!Directory.Exists(_Path)) Directory.CreateDirectory(_Path);
            return _Path;
        }
    }
    const string _Path = "file_registry";

    readonly Dictionary<string, TelegramMediaFile> _fileRegistry = new();

    public string Add(TelegramMediaFile mediaFile)
    {
        var key = Guid.NewGuid().ToString();
        _fileRegistry[key] = mediaFile;
        return key;
    }

    public TelegramMediaFile? TryGet(string key)
    {
        _fileRegistry.TryGetValue(key, out var mediaFile);
        return mediaFile;
    }
}
