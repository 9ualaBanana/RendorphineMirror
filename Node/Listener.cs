using System.Net;
using System.Text;
using System.Text.Json;
using Common.Tasks;
using Common.Tasks.Models;
using Common.Tasks.Tasks;
using Common.Tasks.Tasks.DTO;
using MonoTorrent;
using MonoTorrent.BEncoding;
using MonoTorrent.Client;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Node.P2P;
using Node.Profiler;

namespace Node
{
    // TODO: maybe aspnet instead of this but idk
    public static class Listener
    {
        static readonly Newtonsoft.Json.JsonSerializer JsonSerializerWithTypes = new() { TypeNameHandling = TypeNameHandling.Auto, };
        static readonly HttpClient Client = new();
        static readonly TaskManager TaskManager = new();

        static JObject JsonFromOpResult(in OperationResult result)
        {
            var json = new JObject() { ["ok"] = new JValue(result.Success), };
            if (!result) json["errormsg"] = result.AsString();

            return json;
        }
        static JObject JsonFromOpResult<T>(in OperationResult<T> result)
        {
            var json = JsonFromOpResult(result.EString);
            if (result) json["value"] = JToken.FromObject(result.Value!);

            return json;
        }
        static JObject JsonFromOpResult(JToken token)
        {
            var json = JsonFromOpResult((OperationResult) true);
            json["value"] = token;

            return json;
        }
        static Task<HttpStatusCode> WriteSuccess(HttpListenerResponse response) => _Write(response, JsonFromOpResult((OperationResult) true));
        static Task<HttpStatusCode> WriteJson<T>(HttpListenerResponse response, in OperationResult<T> result) => _Write(response, JsonFromOpResult(result));
        static Task<HttpStatusCode> WriteJson(HttpListenerResponse response, in OperationResult result) => _Write(response, JsonFromOpResult(result));
        static Task<HttpStatusCode> WriteJToken(HttpListenerResponse response, JToken json) => _Write(response, JsonFromOpResult(json));

        static async Task<HttpStatusCode> _Write(HttpListenerResponse response, JObject json, HttpStatusCode code = HttpStatusCode.OK)
        {
            using var writer = new StreamWriter(response.OutputStream, leaveOpen: true);
            using var jwriter = new JsonTextWriter(writer) { CloseOutput = false };
            await json.WriteToAsync(jwriter).ConfigureAwait(false);

            return code;
        }

        static Task<HttpStatusCode> WriteErr(HttpListenerResponse response, string text) => WriteJson(response, OperationResult.Err(text));
        static Task<HttpStatusCode> WriteText(HttpListenerResponse response, string text, HttpStatusCode code = HttpStatusCode.OK) => Write(response, Encoding.UTF8.GetBytes(text), code);

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

        static void LogRequest(HttpListenerRequest request) => Log.Verbose(@$"{request.RemoteEndPoint} {request.HttpMethod} {request.RawUrl}");
        static async ValueTask Start(string prefix, Func<HttpListenerContext, ValueTask> func)
        {
            var listener = new HttpListener();
            listener.Prefixes.Add(prefix);
            try
            {
                listener.Start();
                Log.Information(@$"HTTP listener started @ {string.Join(", ", listener.Prefixes)}");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "HTTP listener was unable to start with the following prefixes: {Prefixes}",
                    string.Join(", ", listener.Prefixes));
            }

            while (true)
            {
                HttpListenerContext? context = null;
                try
                {
                    context = await listener.GetContextAsync().ConfigureAwait(false);
                    await func(context).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    Log.Error(ex.ToString());

                    try
                    {
                        if (context is not null)
                            await WriteText(context.Response, ex.Message, HttpStatusCode.InternalServerError).ConfigureAwait(false);
                    }
                    catch { }
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

                if (subpath == "ping") return HttpStatusCode.OK;

                var query = request.QueryString;

                if (subpath == "uploadtorrent")
                {
                    return await Test(request, response, "url", "dir", async (url, dir) =>
                    {
                        var peerid = TorrentClient.PeerId.UrlEncode();
                        var peerurl = PortForwarding.GetPublicIPAsync().ConfigureAwait(false);
                        var (data, manager) = await TorrentClient.CreateAddTorrent(dir).ConfigureAwait(false);
                        var downloadr = await LocalApi.Post(url, $"downloadtorrent?peerid={peerid}&peerurl={await peerurl}:{TorrentClient.ListenPort}", new ByteArrayContent(data)).ConfigureAwait(false);
                        if (!downloadr) return await WriteJson(response, downloadr).ConfigureAwait(false);

                        return await WriteJson(response, manager.InfoHash.ToHex().AsOpResult()).ConfigureAwait(false);
                    }).ConfigureAwait(false);
                }

                if (subpath == "reloadcfg") Settings.Reload();

                if (subpath == "setnick")
                {
                    return await Test(request, response, "nick", async nick =>
                    {
                        OperationResult resp;
                        lock (NodeProfiler.HeatbeatLock)
                        {
                            resp = SessionManager.RenameServerAsync(nick).ConfigureAwait(false).GetAwaiter().GetResult();
                            if (resp) Settings.NodeName = nick;
                        }

                        return await WriteJson(response, resp).ConfigureAwait(false);
                    }).ConfigureAwait(false);
                }
                if (subpath == "setcfg")
                {
                    return await Test(request, response, "key", "value", async (key, value) =>
                    {
                        var bindable = Settings.Bindables.FirstOrDefault(x => x.Name == key);
                        if (bindable is null) return await WriteErr(response, "no such cfg").ConfigureAwait(false);

                        try { bindable.SetFromJson(value); }
                        catch (Exception ex) { return await WriteErr(response, "invalid value: " + ex.Message).ConfigureAwait(false); }

                        return await writeConfig().ConfigureAwait(false);
                    }).ConfigureAwait(false);
                }
                if (subpath == "getcfg") return await writeConfig().ConfigureAwait(false);

                if (subpath == "getstate")
                    return await WriteJToken(response, JToken.FromObject(new { State = GlobalState.State }, JsonSerializerWithTypes)).ConfigureAwait(false);

                return HttpStatusCode.NotFound;


                async Task<HttpStatusCode> writeConfig()
                {
                    var cfg = new JObject();
                    foreach (var setting in Settings.Bindables)
                        if (!setting.Hidden)
                            cfg[setting.Name] = setting.ToJson();

                    return await WriteJToken(response, cfg).ConfigureAwait(false);
                }
            }
            async ValueTask<HttpStatusCode> post(HttpListenerRequest request, string[] segments, HttpListenerResponse response)
            {
                await Task.Yield(); // TODO: remove
                var subpath = segments[0].ToLowerInvariant();

                if (subpath == "starttask")
                {
                    var token = await JToken.LoadAsync(new JsonTextReader(new StreamReader(request.InputStream))).ConfigureAwait(false);
                    // File.WriteAllText("/tmp/a", token.ToString());

                    var upload = token["upload"]!.ToObject<Uploadp>()!;

                    var data = token["data"]!.ToObject<IPluginActionData>(JsonSerializerWithTypes)!;
                    var outputdir = token["outputdir"]!.ToObject<string>()!;
                    var outputfilename = token["outputfilename"]!.ToObject<string>()!;

                    var task = new NodeTask<IPluginActionData>(data, new TaskObject(upload.FileName, upload.UploadedBytesCount), new MPlusTaskInputInfo(upload.FileId), new MPlusTaskOutputInfo(outputfilename, outputdir));
                    var taskid = await TaskManager.RegisterTaskAsync(task).ConfigureAwait(false);
                    await WriteJson(response, taskid.AsOpResult()).ConfigureAwait(false);

                    await Task.Delay(1000).ConfigureAwait(false);
                    var state = await Api.ApiGet($"{Api.TaskManagerEndpoint}/getmytaskstate", null, ("sessionid", Settings.SessionId!), ("taskid", taskid)).ConfigureAwait(false);

                    return HttpStatusCode.OK;
                }

                return HttpStatusCode.NotFound;
            }
        }
        record Uploadp(string FileId, string FileName, long UploadedBytesCount);


        public static ValueTask StartPublicListenerAsync() => Start(@$"http://*:{PortForwarding.Port}/", PublicListener);
        static ValueTask PublicListener(HttpListenerContext context)
        {
            return Execute(context, get, post);


            async ValueTask<HttpStatusCode> get(HttpListenerRequest request, string[] segments, HttpListenerResponse response)
            {
                var subpath = segments[0].ToLowerInvariant();

                if (subpath == "ping")
                    return await WriteText(response, $"ok from {MachineInfo.PCName} {MachineInfo.UserName} v{MachineInfo.Version}", HttpStatusCode.OK).ConfigureAwait(false);

                var query = request.QueryString;

                if (subpath == "torrentinfo")
                {
                    return await Test(request, response, "hash", async (hash) =>
                    {
                        var ihash = InfoHash.FromHex(hash);
                        var manager = TorrentClient.TryGet(ihash);
                        if (manager is null) return await WriteErr(response, "no such torrent").ConfigureAwait(false);

                        var data = new JObject()
                        {
                            ["peers"] = JObject.FromObject(manager.Peers),
                            ["progress"] = new JValue(manager.PartialProgress),
                            ["monitor"] = JObject.FromObject(manager.Monitor),
                        };

                        return await WriteJToken(response, data).ConfigureAwait(false);
                    }).ConfigureAwait(false);
                }
                if (subpath == "stoptorrent")
                {
                    return await Test(request, response, "hash", async (hash) =>
                    {
                        var ihash = InfoHash.FromHex(hash);
                        var manager = TorrentClient.TryGet(ihash);
                        if (manager is null) return await WriteErr(response, "no such torrent").ConfigureAwait(false);

                        Log.Information($"Stopping torrent {hash}");
                        await manager.StopAsync().ConfigureAwait(false);
                        await TorrentClient.Client.RemoveAsync(manager).ConfigureAwait(false);

                        return await WriteSuccess(response).ConfigureAwait(false);
                    }).ConfigureAwait(false);
                }

                return HttpStatusCode.NotFound;
            }
            async ValueTask<HttpStatusCode> post(HttpListenerRequest request, string[] segments, HttpListenerResponse response)
            {
                var subpath = segments[0].ToLowerInvariant();

                var query = request.QueryString;

                if (subpath == "downloadtorrent")
                {
                    return await Test(request, response, "peerurl", "peerid", async (peerurl, peerid) =>
                    {
                        // using memorystream since request.InputStream doesnt support seeking
                        var stream = new MemoryStream();
                        await request.InputStream.CopyToAsync(stream).ConfigureAwait(false);
                        stream.Position = 0;

                        var torrent = await Torrent.LoadAsync(stream).ConfigureAwait(false);
                        var manager = await TorrentClient.AddOrGetTorrent(torrent, "torrenttest_" + torrent.InfoHash.ToHex()).ConfigureAwait(false);

                        Log.Debug(@$"Downloading torrent {torrent.InfoHash.ToHex()} from peer {peerurl}");

                        var peer = new Peer(BEncodedString.FromUrlEncodedString(peerid), new Uri("ipv4://" + peerurl));
                        await manager.AddPeerAsync(peer).ConfigureAwait(false);

                        return await WriteSuccess(response).ConfigureAwait(false);
                    }).ConfigureAwait(false);
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


        static Task<HttpStatusCode> WriteNoArgument(HttpListenerResponse response, string key) => WriteErr(response, "no " + key);
        static Task<HttpStatusCode> Test(HttpListenerRequest request, HttpListenerResponse response, string c1, Func<string, Task<HttpStatusCode>> func)
        {
            var c1v = request.QueryString[c1];
            if (c1v is null) return WriteNoArgument(response, c1);

            return func(c1v);
        }
        static Task<HttpStatusCode> Test(HttpListenerRequest request, HttpListenerResponse response, string c1, string c2, Func<string, string, Task<HttpStatusCode>> func) =>
            Test(request, response, c1, c1v =>
            {
                var c2v = request.QueryString[c2];
                if (c2v is null) return WriteNoArgument(response, c2);

                return func(c1v, c2v);
            });
    }
}