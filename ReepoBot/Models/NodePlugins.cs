using Machine;

namespace ReepoBot.Models;

public record struct NodePlugins(
    MachineInfo.DTO NodeInfo,
    HashSet<Plugin> Plugins)
{
}
