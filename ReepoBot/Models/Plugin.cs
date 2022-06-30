using Common.Tasks;
using Machine.Plugins;

namespace ReepoBot.Models;

public record struct Plugin(
    PluginType Type,
    string Version,
    string Path)
{
}
