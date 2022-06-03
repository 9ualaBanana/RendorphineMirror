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
                Device.CreatePortMap(new Mapping(Protocol.Tcp, Settings.UPnpPort, Settings.UPnpPort, 0, "renderphine"));

                Mapping = Device.GetSpecificMapping(Protocol.Tcp, Settings.UPnpPort);
                Console.WriteLine("Found UPnP mapping: " + Mapping.ToString());
            }
        }

        public static Task<IPAddress> GetPublicIPAsync() => Device?.GetExternalIPAsync() ?? FetchPublicIpAsync();


        static readonly ImmutableArray<string> IpServices = new[]
        {
            "https://ipv4.icanhazip.com",
            "https://api.ipify.org",
            "https://ipinfo.io/ip",
            "https://checkip.amazonaws.com",
            "https://wtfismyip.com/text",
            "http://icanhazip.com",
        }.ToImmutableArray();
        static async Task<IPAddress> FetchPublicIpAsync()
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