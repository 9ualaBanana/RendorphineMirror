namespace ReepoBot.Services.Telegram.FileRegistry;

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

    readonly Dictionary<string, string> _fileRegistry = new();

    public string Add(string fileId)
    {
        var key = Guid.NewGuid().ToString();
        _fileRegistry[key] = fileId;
        return key;
    }

    public string? TryGet(string key)
    {
        _fileRegistry.TryGetValue(key, out var fileId);
        return fileId;
    }
}
