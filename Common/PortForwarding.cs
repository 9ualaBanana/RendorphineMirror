using System.Collections.Immutable;
using System.Net;
using Mono.Nat;

namespace Common
{
    public static class PortForwarding
    {
        public static bool Initialized { get; private set; }
        public static int Port => Settings.UPnpPort;
        static INatDevice? Device;

        // empty method to trigger static ctor
        public static void Initialize() { }
        static PortForwarding()
        {
            NatUtility.DeviceFound += found;
            NatUtility.StartDiscovery(NatProtocol.Upnp);


            static void found(object? _, DeviceEventArgs args)
            {
                Device = args.Device;

                try
                {
                    var mappings = Device.GetAllMappings();
                    map(mappings, "renderphine", Settings.BUPnpPort);
                    map(mappings, "renderphine-dht", Settings.BDhtPort);
                    map(mappings, "renderphine-trt", Settings.BTorrentPort);
                }
                catch (Exception ex)
                {
                    Log.Error("[UPnP] Could not create mapping: " + ex.Message);
                    Initialized = false;
                }
            }

            static void map(Mapping[] mappings, string name, Bindable<ushort> portb)
            {
                name += "-" + Environment.MachineName;
                var mapping = mappings.FirstOrDefault(x => x.Description == name);
                if (mapping is not null)
                {
                    Log.Information($"[UPnP] Found already existing mapping: {mapping}");
                    portb.Value = (ushort) mapping.PublicPort;
                    Initialized = true;
                }
                else
                {
                    Log.Information($"[UPnP] Could not find valid existing mapping");

                    try
                    {
                        for (ushort port = portb.Value; port < ushort.MaxValue; port++)
                        {
                            if (mappings.Any(x => x.PublicPort == port)) continue;

                            Log.Information($"[UPnP] Creating mapping on port {port} with name {name}");
                            portb.Value = port;
                            Device.CreatePortMap(new Mapping(Protocol.Tcp, port, port, 0, name));
                            Initialized = true;

                            break;
                        }
                    }
                    catch (MappingException ex) { Log.Error(ex.Message); }
                }
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