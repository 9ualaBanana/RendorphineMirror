using System.Net;
using System.Net.Sockets;

namespace NodeToUI
{
    public static class PortForwarding
    {
        public static int Port => Settings.UPnpPort;
        public static int ServerPort => Settings.UPnpServerPort;

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
        public static async Task<IPAddress> GetPublicIPAsync(CancellationToken token = default)
        {
            if (CachedPublicIp is not null && IpCacheTime > DateTime.Now)
                return CachedPublicIp;

            using var client = new HttpClient();
            foreach (var service in IpServices)
            {
                try
                {
                    var ip = await client.GetStringAsync(service).ConfigureAwait(false);

                    CachedPublicIp = IPAddress.Parse(ip.Trim());
                    IpCacheTime = DateTime.Now.AddHours(1);

                    return CachedPublicIp;
                }
                catch { }
            }

            throw new Exception("Could not fetch extenal IP");
        }

        public static async Task<bool> IsPortOpenAndListening(string host, int port, CancellationToken token = default)
        {
            var task = new TaskCompletionSource<bool>(true);

            var ar = new SocketAsyncEventArgs() { RemoteEndPoint = new IPEndPoint(IPAddress.Loopback, port) };
            ar.Completed += (obj, e) =>
            {
                task.SetResult(e.SocketError == SocketError.Success);
                ar.Dispose();
            };
            Socket.ConnectAsync(SocketType.Stream, ProtocolType.Tcp, ar);

            return await task.Task;
        }
    }
}