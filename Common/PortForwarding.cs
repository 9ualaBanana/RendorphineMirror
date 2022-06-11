using System.Collections.Immutable;
using System.Net;
using Mono.Nat;

namespace Common
{
    public static class PortForwarding
    {
        public static bool Initialized => Mapping is not null;
        public static int Port => Mapping?.PublicPort ?? Settings.UPnpPort;
        static INatDevice? Device;
        static Mapping? Mapping;

        public static void Initialize()
        {
            NatUtility.DeviceFound += found;
            NatUtility.StartDiscovery(NatProtocol.Upnp);


            static void found(object? _, DeviceEventArgs args)
            {
                if (Mapping is not null && !Mapping.IsExpired()) return;

                Device = args.Device;

                var name = "renderphine-" + Environment.MachineName;
                var mappings = Device.GetAllMappings();
                var mapping = mappings.FirstOrDefault(x => x.Description == name);
                if (mapping is not null)
                {
                    Log.Information($"[UPnP] Found already existing mapping: {mapping}");
                    Settings.UPnpPort = (ushort) mapping.PublicPort;
                }
                else
                {
                    Log.Information($"[UPnP] Could not find valid existing mapping");

                    try
                    {
                        for (ushort port = Settings.UPnpPort; port < ushort.MaxValue; port++)
                        {
                            if (mappings.Any(x => x.PublicPort == port)) continue;

                            Log.Information($"[UPnP] Creating mapping on port {port} with name {name}");
                            Settings.UPnpPort = port;
                            Device.CreatePortMap(new Mapping(Protocol.Tcp, port, port, 0, name));
                            break;
                        }
                    }
                    catch (MappingException ex) { Log.Error(ex.Message); }
                }

                Mapping = Device.GetSpecificMapping(Protocol.Tcp, Settings.UPnpPort);
            }
        }

        static readonly ImmutableArray<string> IpServices = new[]
        {
            "https://ipv4.icanhazip.com",
            "https://api.ipify.org",
            "https://ipinfo.io/ip",
            "https://checkip.amazonaws.com",
            "https://wtfismyip.com/text",
            "http://icanhazip.com",
        }.ToImmutableArray();
        public static async Task<IPAddress> GetPublicIPAsync()
        {
            using var client = new HttpClient();
            foreach (var service in IpServices)
            {
                try { return IPAddress.Parse(await client.GetStringAsync(service).ConfigureAwait(false)); }
                catch { }
            }

            throw new Exception("Could not fetch extenal IP");
        }
    }
}