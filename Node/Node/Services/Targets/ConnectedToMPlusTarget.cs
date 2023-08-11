using Node.Heartbeat;

namespace Node.Services.Targets;

/// <summary>
/// Enables communication with M+; Starts m+ and tgbot heartbeats
/// </summary>
public class ConnectedToMPlusTarget : IServiceTarget
{
    public static void CreateRegistrations(ContainerBuilder builder)
    {
        builder.RegisterType<PortForwarder>()
            .SingleInstance()
            .OnActivating(p => p.Instance.Start());

        builder.RegisterType<MPlusHeartbeat>()
            .OnActivating(h => h.Instance.Start())
            .SingleInstance();

        builder.RegisterType<TelegramBotHeartbeat>()
            .OnActivating(h => h.Instance.Start())
            .SingleInstance();
    }

    public required ReconnectTarget Reconnect { get; init; }
    public required PortForwarder PortForwarder { get; init; }
    public required MPlusHeartbeat MPlusHeartbeat { get; init; }
    public required TelegramBotHeartbeat TelegramBotHeartbeat { get; init; }

    public async Task ExecuteAsync()
    {
        PortForwarder.Start();
        MPlusHeartbeat.Start();
        TelegramBotHeartbeat.Start();
    }
}
