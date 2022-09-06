using Telegram.Bot.Types.InputFiles;

namespace Telegram.Services.Telegram.FileRegistry;

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

    readonly Dictionary<string, InputOnlineFile> _fileRegistry = new();

    public string Add(InputOnlineFile inputOnlineFile)
    {
        var key = Guid.NewGuid().ToString();
        _fileRegistry[key] = inputOnlineFile;
        return key;
    }

    public InputOnlineFile? TryGet(string key)
    {
        _fileRegistry.TryGetValue(key, out var inputOnlineFile);
        return inputOnlineFile;
    }
}
