using System.Net;
using System.Text;
using System.Web;
using Node.Listeners;

namespace Node.Tasks.Watching.Handlers.Input;

public class OneClickWatchingTaskInputHandler : WatchingTaskInputHandler<OneClickWatchingTaskInputInfo>, ITypedTaskWatchingInput
{
    public static WatchingTaskInputType Type => WatchingTaskInputType.OneClick;

    public required IPluginList PluginList { get; init; }
    public required OCLocalListener LocalListener { get; init; }
    public required OCPublicListener PublicListener { get; init; }
    public required RFProduct.Factory RFProductFactory { get; init; }
    public required IRFProductStorage RFProducts { get; init; }
    public required IComponentContext Container { get; init; }

    public override void StartListening() => StartThreadRepeated(5_000, RunOnce);

    public async Task RunOnce()
    {
        if (!string.IsNullOrWhiteSpace(Input.TestMzpDirectory))
        {
            Directory.CreateDirectory(Input.TestMzpDirectory);

            try
            {
                var betamzp = Directory.GetFiles(Input.TestMzpDirectory)
                    .Where(p => Path.GetFileName(p).StartsWith("oneclick") && p.EndsWith(".mzp"))
                    .Max();

                if (betamzp is not null)
                {
                    var plugin = new Plugin(PluginType.OneClick, Path.GetFileNameWithoutExtension(betamzp)!.Substring("oneclickexport.v".Length), betamzp);
                    await CreateRunner(plugin, true).Run();
                    return;
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
        }

        try
        {
            var plugin = PluginList.GetPlugin(PluginType.OneClick);
            await CreateRunner(plugin, false).Run();
        }
        catch (Exception ex)
        {
            Logger.Error(ex);
        }
    }

    OneClickRunner CreateRunner(Plugin? oneClickPlugin, bool test)
    {
        return new OneClickRunner(Input, test)
        {
            WatchingTask = Task,
            PluginList = PluginList,
            TdsMaxPlugin = PluginList.GetPlugin(PluginType.Autodesk3dsMax, pl => pl.Version != "2024"),
            OneClickPlugin = oneClickPlugin ?? PluginList.GetPlugin(PluginType.OneClick),
            SaveFunc = SaveTask,
            LocalListener = LocalListener,
            RFProductFactory = RFProductFactory,
            RFProducts = RFProducts,
            Container = Container,
            Logger = Logger,
        };
    }


    [AutoRegisteredService(true)]
    public class OCLocalListener : ExecutableListenerBase
    {
        TaskCompletionSource<ProductJson>? Completion;

        protected override ListenTypes ListenType => ListenTypes.Local;
        protected override string? Prefix => "oc";

        public required IWatchingTasksStorage WatchingTasks { get; init; }

        public OCLocalListener(ILogger<OCLocalListener> logger) : base(logger) { }

        public async Task<ProductJson> WaitForCompletion(Action<ProductJson> onReceiveFunc, TimeSpan timeout, CancellationToken token)
        {
            while (true)
            {
                try
                {
                    Completion = new();

                    var cancellation = CancellationTokenSource.CreateLinkedTokenSource(token, new CancellationTokenSource(timeout).Token);
                    onReceiveFunc(await Completion.Task.WaitAsync(cancellation.Token));
                }
                finally
                {
                    Completion = null;
                }
            }
        }

        protected override async Task<HttpStatusCode> ExecuteGet(string path, HttpListenerContext context)
        {
            var request = context.Request;
            var response = context.Response;

            if (path.StartsWith("getnodeinfo", StringComparison.Ordinal))
            {
                return await WriteJsonInline(response, new
                {
                    userid = Settings.UserId,
                    publicport = Settings.UPnpServerPort,
                }.AsOpResult());
            }

            if (path == "unpause")
            {
                var oc = WatchingTasks.WatchingTasks.Values
                    .First(t => t.Source is OneClickWatchingTaskInputInfo);
                oc.IsPaused = false;
                WatchingTasks.WatchingTasks.Bindable.TriggerValueChanged();
                WatchingTasks.WatchingTasks.Save(oc);

                return await WriteSuccess(response).ConfigureAwait(false);
            }
            if (path == "pause")
            {
                var oc = WatchingTasks.WatchingTasks.Values
                    .First(t => t.Source is OneClickWatchingTaskInputInfo);
                oc.IsPaused = true;
                WatchingTasks.WatchingTasks.Bindable.TriggerValueChanged();
                WatchingTasks.WatchingTasks.Save(oc);

                return await WriteSuccess(response).ConfigureAwait(false);
            }
            if (path == "setautocreaterfp")
            {
                return await Test(request, response, "enabled", async enabledstr =>
                {
                    var enabled = JsonConvert.DeserializeObject<bool>(enabledstr);

                    var oc = WatchingTasks.WatchingTasks.Values
                        .First(t => t.Source is OneClickWatchingTaskInputInfo);
                    ((OneClickWatchingTaskInputInfo) oc.Source).AutoCreateRFProducts = enabled;
                    WatchingTasks.WatchingTasks.Bindable.TriggerValueChanged();
                    WatchingTasks.WatchingTasks.Save(oc);

                    return await WriteSuccess(response).ConfigureAwait(false);
                }).ConfigureAwait(false);
            }
            if (path == "setautopublishrfp")
            {
                return await Test(request, response, "enabled", async enabledstr =>
                {
                    var enabled = JsonConvert.DeserializeObject<bool>(enabledstr);

                    var oc = WatchingTasks.WatchingTasks.Values
                        .First(t => t.Source is OneClickWatchingTaskInputInfo);
                    ((OneClickWatchingTaskInputInfo) oc.Source).AutoPublishRFProducts = enabled;
                    WatchingTasks.WatchingTasks.Bindable.TriggerValueChanged();
                    WatchingTasks.WatchingTasks.Save(oc);

                    return await WriteSuccess(response).ConfigureAwait(false);
                }).ConfigureAwait(false);
            }

            return await base.ExecuteGet(path, context);
        }
        protected override async Task<HttpStatusCode> ExecutePost(string path, HttpListenerContext context)
        {
            if (path.StartsWith("JobCompleted", StringComparison.Ordinal))
            {
                var productJson = (await JObject.LoadAsync(new JsonTextReader(new StreamReader(context.Request.InputStream)))).ToObject<ProductJson>().ThrowIfNull();
                Completion.ThrowIfNull("No products waiting to be received.")
                    .SetResult(productJson);

                Completion = null;
            }
            if (path.StartsWith("JobError", StringComparison.Ordinal))
            {
                Completion.ThrowIfNull("No products waiting to be errored.")
                    .SetException(new Exception(await new StreamReader(context.Request.InputStream).ReadToEndAsync()));

                Completion = null;
            }

            return await base.ExecutePost(path, context);
        }
    }

    [AutoRegisteredService(true)]
    public class OCPublicListener : ExecutableListenerBase
    {
        protected override ListenTypes ListenType => ListenTypes.WebServer;
        protected override string? Prefix => "oc";

        public required WatchingTasksHandler WatchingTasksHandler { get; init; }
        public required IWatchingTasksStorage WatchingTasks { get; init; }

        public OCPublicListener(ILogger<OCPublicListener> logger) : base(logger) { }

        protected override async Task<HttpStatusCode> ExecuteGet(string path, HttpListenerContext context)
        {
            var response = context.Response;

            var task = WatchingTasks.WatchingTasks.Values.First(d => d.Source is OneClickWatchingTaskInputInfo);
            var handler = WatchingTasksHandler.GetHandler<OneClickWatchingTaskInputHandler>(task);
            var runner = new OneClickRunnerInfo(handler.Input);

            if (path == "getproducts")
            {
                return await CheckSendAuthentication(context, async () =>
                {
                    return await WriteJsonInline(response, new
                    {
                        products = runner.GetExportInfosByArchiveFiles(Directory.GetFiles(handler.Input.InputDirectory))
                            .SelectMany(info => info.Unity?.Values.Select(u => u.ProductInfo) ?? Enumerable.Empty<ProductJson>())
                            .WhereNotNull()
                            .ToArray(),
                    }.AsOpResult());
                });
            }

            if (path == "getexportstatus")
            {
                return await CheckSendAuthentication(context, async () =>
                {
                    return await WriteJsonInline(response, new
                    {
                        archives = Directory.GetFiles(runner.InputDir).Select(Path.GetFileName).ToArray(),
                        export = runner.GetExportInfosByArchiveFilesDict(Directory.GetFiles(handler.Input.InputDirectory)),
                    }.AsOpResult());
                });
            }

            void getLogs(string archiveFileName, ProjectExportInfo exportInfo, out List<string> maxlogs, out List<string> unitylogs, out List<string> unitylogs2)
            {
                maxlogs = [];
                unitylogs = [];
                unitylogs2 = [];

                try { maxlogs.AddRange(Directory.GetFiles(handler.Input.LogDirectory).Where(f => f.ContainsOrdinal(Path.GetFileNameWithoutExtension(archiveFileName)))); }
                catch { }
                try { unitylogs.AddRange(Directory.GetFiles(Path.Combine(handler.Input.LogDirectory, "unity")).Where(f => f.ContainsOrdinal(Path.GetFileNameWithoutExtension(archiveFileName)))); }
                catch { }

                try
                {
                    foreach (var dir in Directory.GetDirectories(Path.Combine(handler.Input.OutputDirectory)))
                    {
                        var ddir = Path.Combine(dir, "unity", "Assets", exportInfo.ProductName, exportInfo.ProductName + ".log");
                        if (File.Exists(ddir))
                            unitylogs2.Add(ddir);
                    }
                }
                catch { }
            }

            if (path == "getexportlogs")
            {
                return await CheckSendAuthentication(context, async () =>
                {
                    var archive = HttpUtility.ParseQueryString(context.Request.Url.ThrowIfNull().Query)["archive"].ThrowIfNull();
                    var exportinfo = runner.GetExportInfosByArchiveFiles([archive]);
                    getLogs(archive, exportinfo.First(), out var maxlogs, out var unitylogs, out var unitylogs2);

                    var resp = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(new { maxlogs, unitylogs, unitylogs2 }));
                    response.ContentLength64 = resp.Length;
                    await response.OutputStream.WriteAsync(resp);

                    return HttpStatusCode.OK;
                });
            }
            if (path == "getexportlog")
            {
                return await CheckSendAuthentication(context, async () =>
                {
                    var archive = HttpUtility.ParseQueryString(context.Request.Url.ThrowIfNull().Query)["archive"].ThrowIfNull();
                    var type = HttpUtility.ParseQueryString(context.Request.Url.ThrowIfNull().Query)["type"].ThrowIfNull();
                    var fileidx = int.Parse(HttpUtility.ParseQueryString(context.Request.Url.ThrowIfNull().Query)["file"].ThrowIfNull(), CultureInfo.InvariantCulture);

                    var exportinfo = runner.GetExportInfosByArchiveFiles([archive]);
                    getLogs(archive, exportinfo.First(), out var maxlogs, out var unitylogs, out var unitylogs2);


                    var items = type switch
                    {
                        "max" => maxlogs,
                        "unity" => unitylogs,
                        "unity2" => unitylogs2,
                        _ => throw new Exception("Unknown type")
                    };
                    var item = items[fileidx];

                    var filename = Encoding.UTF8.GetBytes(item + "\n");
                    using var filestream = File.OpenRead(item);
                    response.ContentLength64 = filestream.Length + filename.Length;

                    await response.OutputStream.WriteAsync(filename);
                    await filestream.CopyToAsync(response.OutputStream);
                    return HttpStatusCode.OK;
                });
            }

            return await base.ExecuteGet(path, context);
        }
    }
}
