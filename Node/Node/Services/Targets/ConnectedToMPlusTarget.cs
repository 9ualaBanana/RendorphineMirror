using Node.Heartbeat;

namespace Node.Services.Targets;

/// <summary>
/// Enables communication with M+; Starts m+ and tgbot heartbeats
/// </summary>
public class ConnectedToMPlusTarget : IServiceTarget
{
    public static void CreateRegistrations(ContainerBuilder builder)
    {
        builder.RegisterType<MPlusHeartbeat>()
            .SingleInstance();
    }

    public required ReconnectTarget Reconnect { get; init; }
    public required MPlusHeartbeat MPlusHeartbeat { get; init; }

    void IServiceTarget.Activated() => MPlusHeartbeat.Start();
}
