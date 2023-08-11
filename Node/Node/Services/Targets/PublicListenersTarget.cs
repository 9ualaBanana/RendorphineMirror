using Node.Listeners;

namespace Node.Services.Targets;

public class PublicListenersTarget : IServiceTarget
{
    public static void CreateRegistrations(ContainerBuilder builder)
    {
        builder.RegisterListener<DownloadListener>();
        builder.RegisterListener<PublicListener>();
        builder.RegisterListener<PublicPagesListener>();
        builder.RegisterListener<DirectoryDiffListener>();
    }

    public required DownloadListener DownloadListener { get; init; }
    public required PublicListener PublicListener { get; init; }
    public required PublicPagesListener PublicPagesListener { get; init; }
    public required DirectoryDiffListener DirectoryDiffListener { get; init; }

    public required SettingsInstance Settings { get; init; }
    public required ILogger<PublicListenersTarget> Logger { get; init; }

    public async Task ExecuteAsync()
    {
        PortForwarding.GetPublicIPAsync().ContinueWith(async t =>
        {
            var ip = t.Result.ToString();
            Logger.LogInformation($"Public IP: {ip}; Public port: {Settings.UPnpPort}; Web server port: {Settings.UPnpServerPort}");

            var ports = new[] { Settings.UPnpPort, Settings.UPnpServerPort };
            foreach (var port in ports)
            {
                var open = await PortForwarding.IsPortOpenAndListening(ip, port).ConfigureAwait(false);

                if (open) Logger.LogInformation($"Port {port} is open and listening");
                else Logger.LogError($"Port {port} is either not open or not listening");
            }
        }).Consume();
    }
}
