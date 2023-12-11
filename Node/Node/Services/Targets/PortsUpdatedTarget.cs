namespace Node.Services.Targets;

public static class PortsUpdatedTarget
{
    public abstract class PortsUpdatedTargetBase : IServiceTarget
    {
        protected readonly ILogger Logger;

        protected PortsUpdatedTargetBase(ILogger logger) => Logger = logger;

        public static void CreateRegistrations(ContainerBuilder builder) { }
        Task IServiceTarget.ExecuteAsync() => ExecuteAsync();
        public abstract Task ExecuteAsync();

        /// <summary> Find first available port and set the value of <paramref name="port"/> </summary>
        protected async Task UpdatePort(string ip, DatabaseValue<ushort> port, string description)
        {
            Logger.LogInformation($"Checking {description.ToLowerInvariant()} port {port.Value}");

            while (true)
            {
                var open = await PortForwarding.IsPortOpenAndListening(ip, port.Value, new CancellationTokenSource(TimeSpan.FromSeconds(2)).Token);
                if (!open)
                {
                    Logger.LogInformation($"{description} port: {port.Value}");
                    break;
                }

                Logger.LogWarning($"{description} port {port.Value} is already listening, skipping");
                port.Value++;
            }
        }
    }
    public class LocalPortsUpdatedTarget : PortsUpdatedTargetBase
    {
        public LocalPortsUpdatedTarget(ILogger<LocalPortsUpdatedTarget> logger) : base(logger) { }

        public override async Task ExecuteAsync() =>
            await UpdatePort("127.0.0.1", Settings.BLocalListenPort, "Local");
    }
    public class PublicPortsUpdatedTarget : PortsUpdatedTargetBase
    {
        public PublicPortsUpdatedTarget(ILogger<PublicPortsUpdatedTarget> logger) : base(logger) { }

        public override async Task ExecuteAsync()
        {
            var ip = await PortForwarding.GetPublicIPAsync();
            await UpdatePort(ip.ToString(), Settings.BUPnpPort, "Public");
        }
    }
    public class WebPortsUpdatedTarget : PortsUpdatedTargetBase
    {
        public WebPortsUpdatedTarget(ILogger<WebPortsUpdatedTarget> logger) : base(logger) { }

        public override async Task ExecuteAsync()
        {
            var ip = await PortForwarding.GetPublicIPAsync();
            await UpdatePort(ip.ToString(), Settings.BUPnpServerPort, "Server");
        }
    }
}
