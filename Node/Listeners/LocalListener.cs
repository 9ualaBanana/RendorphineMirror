using System.Net;
using _3DProductsPublish;
using _3DProductsPublish._3DModelDS;
using _3DProductsPublish.CGTrader._3DModelComponents;
using _3DProductsPublish.CGTrader.Network;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Node.Profiling;

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
                var downloadr = await Api.Default.ApiPost($"{url}/downloadtorrent?peerid={peerid}&peerurl={await peerurl}:{Settings.TorrentPort}", "Downloading torrent", new ByteArrayContent(data)).ConfigureAwait(false);
                if (!downloadr) return await WriteJson(response, downloadr).ConfigureAwait(false);

                return await WriteJson(response, manager.InfoHash.ToHex().AsOpResult()).ConfigureAwait(false);
            }).ConfigureAwait(false);
        }

        if (path == "reloadcfg")
        {
            Database.Instance.ReloadAllBindables();
            return await WriteSuccess(response).ConfigureAwait(false);
        }

        if (path == "logout")
        {
            Settings.AuthInfo = null;
            Settings.NodeName = null!;

            return await WriteSuccess(response).ConfigureAwait(false);
        }

        if (path == "setnick")
        {
            return await Test(request, response, "nick", async nick =>
            {
                OperationResult resp;
                using (var _ = Profiler.LockHeartbeat())
                {
                    resp = SessionManager.RenameServerAsync(newname: nick, oldname: Settings.NodeName).ConfigureAwait(false).GetAwaiter().GetResult();
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

                var changed = new { port = Settings.UPnpPort, webport = Settings.UPnpServerPort, torrentport = Settings.TorrentPort, dhtport = Settings.DhtPort };
                _logger.Info($"Settings changed: {JsonConvert.SerializeObject(changed)}");

                _ = Task.Delay(500).ContinueWith(_ => ListenerBase.RestartAll());
                return await WriteSuccess(response).ConfigureAwait(false);
            }).ConfigureAwait(false);
        }

        if (path == "uploadcgtrader")
        {
            return await Test(request, response, "username", "password", "directory", "meta", async (username, password, dir, metastr) =>
            {
                var meta = JsonConvert.DeserializeObject<CGTrader3DProductMetadata>(metastr).ThrowIfNull();
                var model = _3DProduct.FromDirectory(dir, meta);
                var cred = new CGTraderNetworkCredential(username, password, false);

                await _3DProductPublisher.PublishAsync(model, cred);
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

        if (path == "login")
        {
            return await TestPost(await CreateCached(request), response, "login", "password", async (login, password) =>
            {
                var resp = await SessionManager.AuthAsync(login, password);
                return await WriteJson(response, resp).ConfigureAwait(false);
            }).ConfigureAwait(false);
        }
        if (path == "autologin")
        {
            return await TestPost(await CreateCached(request), response, "login", async login =>
            {
                var resp = await SessionManager.AutoAuthAsync(login);
                return await WriteJson(response, resp).ConfigureAwait(false);
            }).ConfigureAwait(false);
        }
        if (path == "weblogin")
        {
            var resp = await SessionManager.WebAuthAsync();
            return await WriteJson(response, resp).ConfigureAwait(false);
        }


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
