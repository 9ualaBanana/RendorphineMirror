using System.Collections.Immutable;
using System.Net;

namespace Common
{
    public static class PortForwarding
    {
        public static int Port => Settings.UPnpPort;

        static IPAddress? CachedPublicIp;
        static DateTime IpCacheTime = DateTime.MinValue;

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
            if (CachedPublicIp is not null && IpCacheTime > DateTime.Now)
                return CachedPublicIp;

            using var client = new HttpClient();
            foreach (var service in IpServices)
            {
                try
                {
                    CachedPublicIp = IPAddress.Parse(await client.GetStringAsync(service).ConfigureAwait(false));
                    IpCacheTime = DateTime.Now.AddHours(1);

                    return CachedPublicIp;
                }
                catch { }
            }

            throw new Exception("Could not fetch extenal IP");
        }
    }
}