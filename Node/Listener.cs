using System.Net;
using System.Text;
using MonoTorrent;
using MonoTorrent.BEncoding;
using MonoTorrent.Client;

namespace Node
{
    public static class Listener
    {
        const HttpStatusCode OK = HttpStatusCode.OK;

        static readonly HttpClient Client = new();

        static async Task Start(string prefix, Func<HttpListenerContext, Task> func)
        {
            var listener = new HttpListener();
            listener.Prefixes.Add(prefix);
            listener.Start();
            Log.Information(@$"HTTP listener started @ {string.Join(", ", listener.Prefixes)}");

            int repeats = 0;
            while (true)
            {
                if (repeats > 3) Environment.Exit(0);

                try
                {
                    var context = await listener.GetContextAsync().ConfigureAwait(false);
                    await func(context).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    repeats++;
                    Log.Error(ex.ToString());
                }
            }
        }

        static Task<HttpStatusCode> WriteErr(HttpListenerResponse response, string text) => Write(response, text, HttpStatusCode.BadRequest);
        static Task<HttpStatusCode> Write(HttpListenerResponse response, string text, HttpStatusCode code) => Write(response, Encoding.UTF8.GetBytes(text), code);
        static async Task<HttpStatusCode> Write(HttpListenerResponse response, ReadOnlyMemory<byte> bytes, HttpStatusCode code)
        {
            await response.OutputStream.WriteAsync(bytes).ConfigureAwait(false); ;
            return code;
        }

        static void LogRequest(HttpListenerRequest request) => Log.Information(@$"{request.RemoteEndPoint} {request.HttpMethod} {request.Url}");


        public static Task StartLocalListenerAsync() => Start(@$"http://127.0.0.1:{Settings.LocalListenPort}/", LocalListener);
        static async Task LocalListener(HttpListenerContext context)
        {
            var request = context.Request;
            if (request.Url is null) return;

            using var response = context.Response;
            using var writer = new StreamWriter(response.OutputStream);

            var segments = request.Url.LocalPath.Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (segments.Length == 0) return;

            response.StatusCode = (int) await execute().ConfigureAwait(false);



            ValueTask<HttpStatusCode> execute()
            {
                LogRequest(request);
                if (request.HttpMethod == "GET") return get();
                if (request.HttpMethod == "POST") return post();

                return HttpStatusCode.NotFound.AsVTask();
            }
            async ValueTask<HttpStatusCode> get()
            {
                await Task.Yield(); // TODO: remove
                var subpath = segments[0].ToLowerInvariant();

                if (subpath == "ping") return OK;


                return HttpStatusCode.NotFound;
            }
            async ValueTask<HttpStatusCode> post()
            {
                await Task.Yield(); // TODO: remove
                var subpath = segments[0].ToLowerInvariant();

                return HttpStatusCode.NotFound;
            }
        }

        public static Task StartPublicListenerAsync() => Start(@$"http://*:{PortForwarding.Port}/", PublicListener);
        static async Task PublicListener(HttpListenerContext context)
        {
            var request = context.Request;
            if (request.Url is null) return;

            using var response = context.Response;
            using var writer = new StreamWriter(response.OutputStream);

            var segments = request.Url.LocalPath.Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (segments.Length == 0) return;

            response.StatusCode = (int) await execute().ConfigureAwait(false);



            ValueTask<HttpStatusCode> execute()
            {
                LogRequest(request);
                if (request.HttpMethod == "GET") return get();
                if (request.HttpMethod == "POST") return post();

                return HttpStatusCode.NotFound.AsVTask();
            }
            async ValueTask<HttpStatusCode> get()
            {
                await Task.Yield(); // TODO: remove
                var subpath = segments[0].ToLowerInvariant();

                if (subpath == "ping")
                    return await Write(response, $"ok from {HardwareInfo.PCName} {HardwareInfo.UserName} v{HardwareInfo.Version}", OK).ConfigureAwait(false);

                return HttpStatusCode.NotFound;
            }
            async ValueTask<HttpStatusCode> post()
            {
                await Task.Yield(); // TODO: remove
                var subpath = segments[0].ToLowerInvariant();

                var query = request.QueryString;

                if (subpath == "downloadtorrent")
                {
                    var peerurl = query["peerurl"];
                    if (peerurl is null) return await WriteErr(response, "no peerurl").ConfigureAwait(false);
                    var peerid = query["peerid"];
                    if (peerid is null) return await WriteErr(response, "no peerid").ConfigureAwait(false);



                    // using memorystream since request.InputStream doesnt support seeking
                    var stream = new MemoryStream();
                    await request.InputStream.CopyToAsync(stream).ConfigureAwait(false);
                    stream.Position = 0;

                    var torrent = await Torrent.LoadAsync(stream).ConfigureAwait(false);
                    var manager = await TorrentClient.AddTorrent(torrent, "torrenttest_" + torrent.InfoHash.ToHex()).ConfigureAwait(false);

                    var peer = new Peer(BEncodedString.FromUrlEncodedString(peerid), new Uri("ipv4://" + peerurl));
                    var success = await manager.AddPeerAsync(peer).ConfigureAwait(false);
                    if (!success)
                    {
                        await TorrentClient.Client.RemoveAsync(manager).ConfigureAwait(false);
                        return await Write(response, $"Could not add a peer {peer}", HttpStatusCode.BadRequest).ConfigureAwait(false);
                    }

                    return OK;
                }


                return HttpStatusCode.NotFound;
            }
        }
    }
}