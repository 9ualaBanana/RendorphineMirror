using System.Net;
using System.Net.Sockets;

namespace Node.Common;

public static class PortForwarding
{
    public const int ASPPort = 5336;

    static IPAddress? CachedPublicIp;
    static DateTime IpCacheTime = DateTime.MinValue;

    static readonly ImmutableArray<string> IpServices =
    [
        "https://ipv4.icanhazip.com",
        "https://api.ipify.org",
        "https://ipinfo.io/ip",
        "https://checkip.amazonaws.com",
        "https://wtfismyip.com/text",
        "http://icanhazip.com",
    ];

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

    public static async Task<(string host, int port)?> TryReadNginxHost(CancellationToken token)
    {
        var dir = "/etc/nginx/sites-enabled";
        if (!Directory.Exists(dir)) return null;

        foreach (var file in Directory.GetFiles(dir))
        {
            var contents = await File.ReadAllTextAsync(file, token);
            var spt = contents.Split("server {");

            var pp = "proxy_pass http://127.0.0.1:";
            foreach (var server in spt)
            {
                var startpp = server.IndexOf(pp);
                if (startpp == -1) continue;
                startpp += pp.Length;

                var endpp = server.IndexOf('/', startpp);
                if (endpp == -1) continue;

                if (!int.TryParse(server.AsSpan(startpp, endpp - startpp), out var ourport))
                    continue;

                int? port = null;
                string? hostname = null;

                foreach (var line in server.Split('\n'))
                {
                    if (line.Contains("listen"))
                    {
                        var portstr = line.Split(' ').SelectMany(l => l.Split(':')).FirstOrDefault(str => int.TryParse(str, out _));
                        if (portstr is null) continue;
                        port = int.Parse(portstr);
                    }
                    else if (line.Contains("server_name"))
                        hostname = line.Split("server_name")[1].Trim().Replace(";", "");

                    if (port is not null && hostname is not null)
                        return (hostname, port.Value);
                }
            }
        }

        return null;
    }

    public static async Task<int> GetPublicFacingPort(INodeSettings settings, CancellationToken token) => (await TryReadNginxHost(token))?.port ?? settings.UPnpPort;

    public static async Task<bool> IsPortOpenAndListening(string host, int port, CancellationToken token = default)
    {
        token = CancellationTokenSource.CreateLinkedTokenSource(token, new CancellationTokenSource(TimeSpan.FromSeconds(1)).Token).Token;
        try
        {
            using var client = new TcpClient();
            await client.ConnectAsync(host, port, token);

            return true;
        }
        catch { return false; }
    }
}
