using System.Net;
using Node.Listeners;

namespace Node.Tasks.Watching.Handlers.Input;

public class OneClickWatchingTaskInputHandler : WatchingTaskInputHandler<OneClickWatchingTaskInputInfo>, ITypedTaskWatchingInput
{
    public static WatchingTaskInputType Type => WatchingTaskInputType.OneClick;

    public required IPluginList PluginList { get; init; }
    public required OCLocalListener LocalListener { get; init; }
    public required OCPublicListener PublicListener { get; init; }

    public override void StartListening()
    {
        StartThreadRepeated(5_000, RunOnce);
        LocalListener.Start();
        PublicListener.Start();
    }

    public async Task RunOnce()
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
                await CreateRunner(plugin, Input.TestInputDirectory, Input.TestOutputDirectory, Input.TestLogDirectory).Run();
                return;
            }
        }
        catch (Exception ex)
        {
            Logger.Error(ex);
        }

        try
        {
            var plugin = PluginList.GetPlugin(PluginType.OneClick);
            await CreateRunner(plugin, Input.InputDirectory, Input.OutputDirectory, Input.LogDirectory).Run();
        }
        catch (Exception ex)
        {
            Logger.Error(ex);
        }
    }

    OneClickRunner CreateRunner(Plugin? oneClickPlugin, string inputdir, string outputdir, string logdir)
    {
        return new OneClickRunner()
        {
            InputDir = Directories.DirCreated(inputdir),
            OutputDir = Directories.DirCreated(outputdir),
            LogDir = Directories.DirCreated(logdir),
            UnityTemplatesDir = @"C:\\OneClickUnityDefaultProjects",
            PluginList = PluginList,
            TdsMaxPlugin = PluginList.GetPlugin(PluginType.Autodesk3dsMax),
            OneClickPlugin = oneClickPlugin ?? PluginList.GetPlugin(PluginType.OneClick),
            Input = Input,
            SaveFunc = SaveTask,
            LocalListener = LocalListener,
            Logger = Logger,
        };
    }


    [AutoRegisteredService(true)]
    public class OCLocalListener : ExecutableListenerBase
    {
        TaskCompletionSource<ProductJson>? Completion;

        protected override ListenTypes ListenType => ListenTypes.Local;
        protected override string? Prefix => "oc";

        public OCLocalListener(ILogger<OCLocalListener> logger) : base(logger) { }

        public async IAsyncEnumerable<ProductJson> WaitForCompletion(int amount, TimeSpan timeout)
        {
            for (int i = 0; i < amount; i++)
            {
                Completion = new();

                var token = new CancellationTokenSource(timeout);
                yield return await Completion.Task.WaitAsync(token.Token);
                Completion = null;
            }
        }
        public async Task<ProductJson> WaitForCompletion2(Action<ProductJson> onReceiveFunc, TimeSpan timeout, CancellationToken token)
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
            var response = context.Response;

            if (path.StartsWith("getnodeinfo", StringComparison.Ordinal))
            {
                return await WriteJsonInline(response, new
                {
                    userid = Settings.UserId,
                    publicport = Settings.UPnpServerPort,
                }.AsOpResult());
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
            var runner = handler.CreateRunner(null, handler.Input.TestInputDirectory, handler.Input.TestOutputDirectory, handler.Input.TestLogDirectory);

            if (path.StartsWith("getproducts", StringComparison.Ordinal))
            {
                return await WriteJsonInline(response, new
                {
                    products = runner.GetExportInfosByArchiveFiles(Directory.GetFiles(handler.Input.InputDirectory))
                        .SelectMany(info => info.Unity?.Values.Select(u => u.ProductInfo) ?? Enumerable.Empty<ProductJson>())
                        .WhereNotNull()
                        .ToArray(),
                }.AsOpResult());
            }

            return await base.ExecuteGet(path, context);
        }
    }
}
