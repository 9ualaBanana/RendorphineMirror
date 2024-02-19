using System.Net;
using System.Reflection;
using _3DProductsPublish;
using _3DProductsPublish._3DProductDS;
using _3DProductsPublish.CGTrader.Upload;
using _3DProductsPublish.Turbosquid;
using _3DProductsPublish.Turbosquid.Upload;
using Node.Profiling;
using Node.Tasks.Exec.Input;
using Node.Tasks.Exec.Output;

namespace Node.Listeners;

public class LocalListener : ExecutableListenerBase
{
    protected override ListenTypes ListenType => ListenTypes.Local;

    public required PluginManager PluginManager { get; init; }
    public required PluginDeployer PluginDeployer { get; init; }
    public required NodeGlobalState NodeGlobalState { get; init; }
    public required SessionManager SessionManager { get; init; }
    public required Profiler Profiler { get; init; }
    public required RFProduct.Factory RFProductFactory { get; init; }
    public required IComponentContext Container { get; init; }
    public required IRFProductStorage RFProducts { get; init; }
    public required SettingsInstance Settings { get; init; }
    public required INodeGui NodeGui { get; init; }
    public required TaskExecutor TaskExecutor { get; init; }
    public required NodeDataDirs Dirs { get; init; }

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

        if (path == "settaskdir")
        {
            return await Test(request, response, "dir", async dir =>
            {
                Settings.TaskProcessingDirectory.Value = string.IsNullOrWhiteSpace(dir) ? null : dir;
                return await WriteSuccess(response).ConfigureAwait(false);
            }).ConfigureAwait(false);
        }

        if (path == "setaccepttasks")
        {
            return await Test(request, response, "accept", async accept =>
            {
                Settings.AcceptTasks.Value = JsonConvert.DeserializeObject<bool>(accept);
                return await WriteSuccess(response).ConfigureAwait(false);
            }).ConfigureAwait(false);
        }

        if (path == "deploy")
        {
            return await Test(request, response, "type", "version", async (type, version) =>
            {
                Task.Run(async () =>
                {
                    var newcount = await PluginDeployer.DeployUninstalled(PluginChecker.GetInstallationTree(NodeGlobalState.Software.Value, type, version), default);
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
                var field = new[] { typeof(Settings), typeof(NodeSettingsInstance) }
                    .SelectMany(type => type.GetFields(BindingFlags.Public | BindingFlags.Static))
                    .Where(f => f.FieldType.IsAssignableTo(typeof(IDatabaseBindable)))
                    .FirstOrDefault(t => t.Name == "B" + key || t.Name == key);

                if (field is null) return await WriteErr(response, "Unknown key");

                var json = JToken.Parse(value);
                ((IDatabaseBindable) field.GetValue(null).ThrowIfNull()).Bindable.LoadFromJson(json, null);

                return await WriteSuccess(response).ConfigureAwait(false);
            }).ConfigureAwait(false);
        }

        if (path == "3dupload")
        {
            return await TestPost(await CreateCached(request), response, "target", "meta", "dir", async (target, jmeta, dir) =>
            {
                var cancellationToken = CancellationToken.None;
                var metadata = JsonConvert.DeserializeObject<_3DProduct.Metadata_>(jmeta).ThrowIfNull();
                var product = _3DProduct.FromDirectory(dir);

                if (target == "turbosquid")
                {
                    var tsp = await product.AsyncWithTurboSquid(metadata, NodeGui, cancellationToken);
                    await Container.Resolve<TurboSquid>().PublishAsync(tsp, cancellationToken);
                    return await WriteSuccess(response).ConfigureAwait(false);
                }
                if (target == "cgtrader")
                {
                    var cgtrader = Container.Resolve<CGTrader3DProductPublisher>();
                    await cgtrader.PublishAsync(product.WithCGTrader(metadata), new NetworkCredential(Settings.CGTraderUsername.Value, Settings.CGTraderPassword.Value), cancellationToken);
                    return await WriteSuccess(response).ConfigureAwait(false);
                }

                return await WriteErr(response, "Invalid target");
            }).ConfigureAwait(false);
        }
        if (path == "fetchturbosquidsales")
        {
            return await TestPost(await CreateCached(request), response, "mpcreds", "turbocreds", async (mpcredsjson, turbocredsjson) =>
            {
                var mpcreds = JsonConvert.DeserializeObject<NetworkCredential>(mpcredsjson).ThrowIfNull();
                var turbocreds = JsonConvert.DeserializeObject<NetworkCredential>(turbocredsjson).ThrowIfNull();

                Settings.MPlusUsername.Value = mpcreds.UserName;
                Settings.MPlusPassword.Value = mpcreds.Password;
                Settings.TurboSquidUsername.Value = turbocreds.UserName;
                Settings.TurboSquidPassword.Value = turbocreds.Password;

                var turbo = await TurboSquid.LogInAsyncUsing(turbocreds, Container.Resolve<INodeGui>(), default);
                await (await MPAnalytics.LoginAsync(mpcreds, default))
                    .SendAsync(turbo.SaleReports.ScanAsync(default), default);

                return await WriteSuccess(response).ConfigureAwait(false);
            }).ConfigureAwait(false);
        }

        if (path == "unsetcreds")
        {
            return await TestPost(await CreateCached(request), response, "target", async (target) =>
            {
                if (target == "MPlus")
                    Settings.MPlusPassword.Value = Settings.MPlusUsername.Value = null;
                else if (target == "TurboSquid")
                    Settings.TurboSquidPassword.Value = Settings.TurboSquidUsername.Value = null;
                else if (target == "CGTrader")
                    Settings.CGTraderPassword.Value = Settings.CGTraderUsername.Value = null;
                else return await WriteErr(response, "Unknown target");

                return await WriteSuccess(response);
            }).ConfigureAwait(false);
        }
        if (path == "setcreds")
        {
            return await TestPost(await CreateCached(request), response, "target", "creds", async (target, credsstr) =>
            {
                var creds = JsonConvert.DeserializeObject<NetworkCredential>(credsstr);
                if (creds is null) return await WriteErr(response, "Could not parse creds");

                if (target == "MPlus")
                {
                    Settings.MPlusPassword.Value = creds.Password;
                    Settings.MPlusUsername.Value = creds.UserName;
                }
                else if (target == "TurboSquid")
                {
                    Settings.TurboSquidPassword.Value = creds.Password;
                    Settings.TurboSquidUsername.Value = creds.UserName;
                }
                else if (target == "CGTrader")
                {
                    Settings.CGTraderPassword.Value = creds.Password;
                    Settings.CGTraderUsername.Value = creds.UserName;
                }
                else return await WriteErr(response, "Unknown target");

                return await WriteSuccess(response);
            }).ConfigureAwait(false);
        }


        if (path == "createrfproduct")
        {
            return await TestPost(await CreateCached(request), response, "idea", "container", async (idea, container) =>
            {
                var product = await RFProductFactory.CreateAsync(idea, container, default);
                return await WriteJson(response, product.AsOpResult()).ConfigureAwait(false);
            }).ConfigureAwait(false);
        }
        if (path == "deleterfproduct")
        {
            return await TestPost(await CreateCached(request), response, "id", async (id) =>
            {
                RFProducts.RFProducts.Remove(id);
                return await WriteSuccess(response).ConfigureAwait(false);
            }).ConfigureAwait(false);
        }

        if (path == "tasktopaz")
        {
            return await TestPost(await CreateCached(request), response, "file", "info", async (file, info) =>
            {
                var input = new TaskFileInput(new ReadOnlyTaskFileList([FileWithFormat.FromFile(file)]), Dirs.TaskOutputDirectory($"local_{Guid.NewGuid()}"));
                var data = JObject.Parse(info).WithProperty("type", TaskAction.Topaz.ToString());

                return await WriteJson(response, new TaskFileOutput(new TaskFileListList("/temp/asd") { new ReadOnlyTaskFileList([FileWithFormat.FromFile("/temp/file.png")]) }).Files.Single().Single().Path.AsOpResult()).ConfigureAwait(false);

                var result = (TaskFileOutput) await TaskExecutor.Execute(input, data, default);
                return await WriteJson(response, result.Files.Single().Single().Path.AsOpResult()).ConfigureAwait(false);
            }).ConfigureAwait(false);
        }

        if (NodeToUI.NodeGui.GuiRequestTypes.ContainsKey(path) && query["reqid"] is { } reqid && NodeGlobalState.Instance.Requests.TryGetValue(reqid, out var guirequest))
        {
            using var reader = new StreamReader(request.InputStream);
            var value = await reader.ReadToEndAsync();
            guirequest.Task.SetResult(JToken.Parse(value)["value"]!);

            return await WriteSuccess(response);
        }

        return HttpStatusCode.NotFound;
    }
}
