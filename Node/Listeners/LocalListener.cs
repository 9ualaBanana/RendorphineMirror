using System.Net;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Node.Profiling;
using Transport.Upload._3DModelsUpload;
using Transport.Upload._3DModelsUpload.CGTrader;
using Transport.Upload._3DModelsUpload.CGTrader._3DModelComponents;

namespace Node.Listeners;

public class LocalListener : ExecutableListenerBase
{
    readonly HttpClient Client = new();
    protected override ListenTypes ListenType => ListenTypes.Local;

    protected override async Task<HttpStatusCode> ExecuteGet(string path, HttpListenerContext context)
    {
        var request = context.Request;
        var response = context.Response;

        if (path == "ping") return HttpStatusCode.OK;

        var query = request.QueryString;

        if (path == "uploadtorrent")
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

        if (path == "reloadcfg")
        {
            Settings.Reload();
            return await WriteSuccess(response).ConfigureAwait(false);
        }

        if (path == "setnick")
        {
            return await Test(request, response, "nick", async nick =>
            {
                OperationResult resp;
                using (var _ = Profiler.LockHeartbeat())
                {
                    resp = SessionManager.RenameServerAsync(nick).ConfigureAwait(false).GetAwaiter().GetResult();
                    if (resp) Settings.NodeName = nick;
                }

                return await WriteJson(response, resp).ConfigureAwait(false);
            }).ConfigureAwait(false);
        }

        if (path == "deploy")
        {
            return await Test(request, response, "type", "version", async (type, version) =>
            {
                new ScriptPluginDeploymentInfo(new PluginToDeploy() { Type = Enum.Parse<PluginType>(type, true), Version = version }).DeployAsync().Consume();
                return await WriteSuccess(response).ConfigureAwait(false);
            }).ConfigureAwait(false);
        }

        if (path == "updateports")
        {
            return await Test(request, response, "port", "webport", "torrentport", "dhtport", async (port, webport, torrentport, dhtport) =>
            {
                Settings.UPnpPort = ushort.Parse(port);
                Settings.UPnpServerPort = ushort.Parse(webport);
                Settings.TorrentPort = ushort.Parse(torrentport);
                Settings.DhtPort = ushort.Parse(dhtport);

                _ = Task.Delay(500).ContinueWith(_ => ListenerBase.RestartAll());
                return await WriteSuccess(response).ConfigureAwait(false);
            }).ConfigureAwait(false);
        }

        if (path == "uploadcgtrader")
        {
            return await Test(request, response, "username", "password", "directory", "meta", async (username, password, dir, metastr) =>
            {
                var meta = JsonConvert.DeserializeObject<CGTrader3DModelMetadata>(metastr).ThrowIfNull();
                var model = Composite3DModel.FromDirectory(dir, meta);
                var cred = new CGTraderNetworkCredential(username, password, false);

                await _3DModelUploader.UploadAsync(Client, cred, model);
                return await WriteSuccess(response).ConfigureAwait(false);
            }).ConfigureAwait(false);
        }


        return HttpStatusCode.NotFound;
    }
    protected override async Task<HttpStatusCode> ExecutePost(string path, HttpListenerContext context)
    {
        var request = context.Request;
        var response = context.Response;

        var query = request.QueryString;

        if (NodeGui.GuiRequestTypes.ContainsKey(path) && query["reqid"] is { } reqid && NodeGlobalState.Instance.Requests.TryGetValue(reqid, out var guirequest))
        {
            using var reader = new StreamReader(request.InputStream);
            var value = await reader.ReadToEndAsync();
            guirequest.Task.SetResult(JToken.Parse(value)["value"]!);

            return await WriteSuccess(response);
        }

        return HttpStatusCode.NotFound;
    }
}
