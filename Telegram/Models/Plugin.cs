using Common;

namespace Telegram.Models;

public record struct Plugin(
    PluginType Type,
    string Version,
    string Path)
{
}
