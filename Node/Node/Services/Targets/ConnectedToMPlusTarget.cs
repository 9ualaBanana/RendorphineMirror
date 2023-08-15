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

        builder.RegisterType<TelegramBotHeartbeat>()
            .SingleInstance();
    }

    public required ReconnectTarget Reconnect { get; init; }
    public required PortsForwardedTarget PortsForwarded { get; init; }
    public required MPlusHeartbeat MPlusHeartbeat { get; init; }
    public required TelegramBotHeartbeat TelegramBotHeartbeat { get; init; }

    public async Task ExecuteAsync()
    {
        MPlusHeartbeat.Start();
        TelegramBotHeartbeat.Start();
    }
}
