using System.Net;
using System.Reflection;
using _3DProductsPublish;
using _3DProductsPublish._3DModelDS;
using _3DProductsPublish.CGTrader._3DModelComponents;
using _3DProductsPublish.CGTrader.Network;
using Node.Profiling;

namespace Node.Listeners;

public class LocalListener : ExecutableListenerBase
{
    protected override ListenTypes ListenType => ListenTypes.Local;

    public required PluginManager PluginManager { get; init; }
    public required PluginChecker PluginChecker { get; init; }
    public required PluginDeployer PluginDeployer { get; init; }
    public required SessionManager SessionManager { get; init; }
    public required Profiler Profiler { get; init; }

    public LocalListener(ILogger<LocalListener> logger) : base(logger) { }

    protected override async Task<HttpStatusCode> ExecuteGet(string path, HttpListenerContext context)
    {
        var request = context.Request;
        var response = context.Response;

        if (path == "ping") return HttpStatusCode.OK;

        var query = request.QueryString;

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
                    resp = await SessionManager.RenameServerAsync(newname: nick, oldname: Settings.NodeName).ConfigureAwait(false);
                    if (resp) Settings.NodeName = nick;
                }

                return await WriteJson(response, resp).ConfigureAwait(false);
            }).ConfigureAwait(false);
        }

        if (path == "deploy")
        {
            return await Test(request, response, "type", "version", async (type, version) =>
            {
                Task.Run(async () =>
                {
                    var newcount = PluginDeployer.DeployUninstalled(PluginChecker.GetInstallationTree(type, version));
                    if (newcount != 0)
                        await PluginManager.RediscoverPluginsAsync();
                }).Consume();

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
                Logger.Info($"Settings changed: {JsonConvert.SerializeObject(changed)}");

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

        if (path == "setsetting")
        {
            return await TestPost(await CreateCached(request), response, "key", "value", async (key, value) =>
            {
                var field = new[] { typeof(Settings), typeof(NodeSettings) }
                    .SelectMany(type => type.GetFields(BindingFlags.Public | BindingFlags.Static))
                    .Where(f => f.FieldType.IsAssignableTo(typeof(IDatabaseBindable)))
                    .FirstOrDefault(t => t.Name == "B" + key || t.Name == key);

                if (field is null) return await WriteErr(response, "Unknown key");

                var json = JToken.Parse(value);
                ((IDatabaseBindable) field.GetValue(null).ThrowIfNull()).Bindable.LoadFromJson(json, null);

                return await WriteSuccess(response).ConfigureAwait(false);
            }).ConfigureAwait(false);
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
