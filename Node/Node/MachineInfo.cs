using System.Net;

namespace Node;

public record MachineInfo(
    string UserId,
    string NodeName,
    string UserName,
    string PCName,
    string Guid,
    string Version,
    string Port,
    string WebServerPort,
    IPAddress IP,
    ImmutableArray<Plugin> InstalledPlugins
);