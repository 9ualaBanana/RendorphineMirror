using System.Net;
using System.Text;
using MonoTorrent;
using MonoTorrent.BEncoding;
using MonoTorrent.Client;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Node
{
    // TODO: maybe aspnet instead of this but idk
    public static class Listener
    {
        const HttpStatusCode OK = HttpStatusCode.OK;

        static readonly HttpClient Client = new();

        static Task<HttpStatusCode> WriteErr(HttpListenerResponse response, string text) => Write(response, text, HttpStatusCode.BadRequest);
        static Task<HttpStatusCode> Write(HttpListenerResponse response, string text, HttpStatusCode code = HttpStatusCode.OK) => Write(response, Encoding.UTF8.GetBytes(text), code);
        static async Task<HttpStatusCode> Write(HttpListenerResponse response, JObject json, HttpStatusCode code = HttpStatusCode.OK)
        {
            using var writer = new StreamWriter(response.OutputStream, leaveOpen: true);
            using var jwriter = new JsonTextWriter(writer) { CloseOutput = false };
            await json.WriteToAsync(jwriter).ConfigureAwait(false);

            return code;
        }
        static async Task<HttpStatusCode> Write(HttpListenerResponse response, ReadOnlyMemory<byte> bytes, HttpStatusCode code = HttpStatusCode.OK)
        {
            await response.OutputStream.WriteAsync(bytes).ConfigureAwait(false); ;
            return code;
        }
        static async Task<HttpStatusCode> Write(HttpListenerResponse response, Stream bytes, HttpStatusCode code = HttpStatusCode.OK)
        {
            await bytes.CopyToAsync(response.OutputStream).ConfigureAwait(false);
            return code;
        }

        static void LogRequest(HttpListenerRequest request) => Log.Information(@$"{request.RemoteEndPoint} {request.HttpMethod} {request.RawUrl}");
        static async ValueTask Start(string prefix, Func<HttpListenerContext, ValueTask> func)
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

        public static ValueTask StartLocalListenerAsync() => Start(@$"http://127.0.0.1:{Settings.LocalListenPort}/", LocalListener);
        static ValueTask LocalListener(HttpListenerContext context)
        {
            return Execute(context, get, post);


            async ValueTask<HttpStatusCode> get(HttpListenerRequest request, string[] segments, HttpListenerResponse response)
            {
                var subpath = segments[0].ToLowerInvariant();

                if (subpath == "ping") return OK;

                var query = request.QueryString;

                if (subpath == "uploadtorrent")
                {
                    var url = query["url"];
                    if (url is null) return await WriteErr(response, "no url").ConfigureAwait(false);
                    var dir = query["dir"];
                    if (dir is null) return await WriteErr(response, "no dir").ConfigureAwait(false);

                    var peerid = TorrentClient.PeerId.UrlEncode();
                    var peerurl = PortForwarding.GetPublicIPAsync().ConfigureAwait(false);
                    var (data, manager) = await TorrentClient.CreateAddTorrent(dir).ConfigureAwait(false);
                    var postresponse = await new HttpClient().PostAsync($"http://{url}/downloadtorrent?peerid={peerid}&peerurl={await peerurl}:{TorrentClient.ListenPort}", new ByteArrayContent(data)).ConfigureAwait(false);
                    if (!postresponse.IsSuccessStatusCode)
                        return await Write(response, await postresponse.Content.ReadAsStreamAsync().ConfigureAwait(false), postresponse.StatusCode).ConfigureAwait(false);

                    return await Write(response, manager.InfoHash.ToArray()).ConfigureAwait(false);
                }

                return HttpStatusCode.NotFound;
            }
            async ValueTask<HttpStatusCode> post(HttpListenerRequest request, string[] segments, HttpListenerResponse response)
            {
                await Task.Yield(); // TODO: remove
                var subpath = segments[0].ToLowerInvariant();

                return HttpStatusCode.NotFound;
            }
        }

        public static ValueTask StartPublicListenerAsync() => Start(@$"http://*:{PortForwarding.Port}/", PublicListener);
        static ValueTask PublicListener(HttpListenerContext context)
        {
            return Execute(context, get, post);


            async ValueTask<HttpStatusCode> get(HttpListenerRequest request, string[] segments, HttpListenerResponse response)
            {
                var subpath = segments[0].ToLowerInvariant();

                if (subpath == "ping")
                    return await Write(response, $"ok from {HardwareInfo.PCName} {HardwareInfo.UserName} v{HardwareInfo.Version}", OK).ConfigureAwait(false);

                var query = request.QueryString;

                if (subpath == "torrentinfo")
                {
                    var hash = query["hash"];
                    if (hash is null) return await WriteErr(response, "no hash").ConfigureAwait(false);

                    var ihash = InfoHash.FromHex(hash);
                    var manager = TorrentClient.Client.Torrents.FirstOrDefault(x => x.InfoHash == ihash);
                    if (manager is null) return await WriteErr(response, "no such torrent").ConfigureAwait(false);

                    var data = new JObject()
                    {
                        ["peers"] = JObject.FromObject(manager.Peers),
                        ["progress"] = new JValue(manager.PartialProgress),
                        ["monitor"] = JObject.FromObject(manager.Monitor),
                    };

                    return await Write(response, data).ConfigureAwait(false);
                }

                return HttpStatusCode.NotFound;
            }
            async ValueTask<HttpStatusCode> post(HttpListenerRequest request, string[] segments, HttpListenerResponse response)
            {
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

                    Log.Debug(@$"Downloading torrent {torrent.InfoHash.ToHex()} from peer {peerurl}");

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


        delegate ValueTask<HttpStatusCode> ExecuteDelegate(HttpListenerRequest request, string[] segments, HttpListenerResponse response);
        static async ValueTask Execute(HttpListenerContext context, ExecuteDelegate getf, ExecuteDelegate postf)
        {
            var request = context.Request;
            if (request.Url is null) return;

            var segments = request.Url.LocalPath.Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (segments.Length == 0) return;

            using var response = context.Response;
            using var stream = response.OutputStream;
            LogRequest(request);
            response.StatusCode = (int) await (request.HttpMethod switch
            {
                "GET" => getf(request, segments, response),
                "POST" => postf(request, segments, response),
                _ => HttpStatusCode.NotFound.AsVTask(),
            }).ConfigureAwait(false);
        }
    }
}