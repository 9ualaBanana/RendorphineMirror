using System.Net;
using System.Net.Sockets;

namespace Node.Common;

public static class PortForwarding
{
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

    // TODO: either get it from the router or have some other check to not cause potential 24h delays
    /// <summary> Fetches public ip. Caches it for 24 hours </summary>
    public static async Task<IPAddress> GetPublicIPAsync(CancellationToken token = default)
    {
        if (CachedPublicIp is not null && IpCacheTime > DateTime.Now)
            return CachedPublicIp;

        using var client = new HttpClient();
        foreach (var service in IpServices)
        {
            try
            {
                var ip = await client.GetStringAsync(service, token).ConfigureAwait(false);

                CachedPublicIp = IPAddress.Parse(ip.Trim());
                IpCacheTime = DateTime.Now.AddHours(24);

                return CachedPublicIp;
            }
            catch { }
        }

        throw new Exception("Could not fetch extenal IP");
    }

    public static async Task<bool> IsPortOpenAndListening(string host, int port, CancellationToken token = default)
    {
        try
        {
            using var client = new TcpClient();
            await client.ConnectAsync(host, port, token);

            return true;
        }
        catch { return false; }
    }
}