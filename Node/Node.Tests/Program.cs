using System.Diagnostics;
using Node.Common;
using Node.Plugins;
using Node.Plugins.Models;
using Node.Tasks;
using Node.Tasks.Exec;
using Node.Tasks.Exec.Actions;
using Node.Tasks.Exec.FFmpeg;
using Node.Tasks.Exec.FFmpeg.Codecs;
using Node.Tasks.Models;
using Node.Tasks.Models.ExecInfo;
using NodeCommon;


Initializer.AppName = "renderfin";
Init.Initialize();

var pluginManager = new PluginManager(PluginDiscoverers.GetAll());
await pluginManager.RediscoverPluginsAsync();

if (false)
{
    var logger = new LoggableLogger(LogManager.GetCurrentClassLogger());
    var input = FileWithFormat.FromFile("/home/i3ym/Downloads/Telegram Desktop/61dc19f37af4207cb6fb6ebb.mov");
    var output = new TaskFileList(TempDirFor("ffmpegtest"));

    var context = new TestContext(await pluginManager.RediscoverPluginsAsync());

    var ffprobe = await FFProbe.Get(input.Path, logger);
    var launcher = new FFmpegLauncher(context.GetPlugin(PluginType.FFmpeg).Path)
    {
        Logger = logger,
        ProgressSetter = new TaskExecutionContextProgressSetterAdapter(context),

        Input = { input.Path },
        VideoFilters = { "scale=iw/2:ih/2" },
        Outputs =
        {
            new FFmpegLauncherOutput()
            {
                Codec = FFmpegLauncher.CodecFromStream(ffprobe.VideoStream),
                Output = output.New(input.Format, "out_downscaled").Path,
            },
        },
    };

    await launcher.Execute();
    Debugger.Break();
}


// await DeployPluginCondaOnly(pluginManager, PluginType.Esrgan, PluginVersion.Empty);

await TestTaskExecution(
    pluginManager,
    new GenerateQSPreview(),
    new QSPreviewInfo(),
    new ReadOnlyTaskFileList(FileWithFormat.FromLocalPath("/home/i3ym/workspace/workdir/dead/fish_grill_02.mov"))
);

// await TestTasksExecution(pluginManager);

Debugger.Break();


static async Task DeployPluginCondaOnly(PluginManager pluginManager, PluginType type, PluginVersion version)
{
    var deployer = new PluginDeployer(new InstalledPluginsProvider((await pluginManager.GetInstalledPluginsAsync()).Where(t => t.Type != type).ToArray()));
    var checker = new PluginChecker(new SoftwareListProvider(await Apis.DefaultWithSessionId("63fe288368974192c27a5388").GetSoftwareAsync().ThrowIfError()));
    deployer.DeployUninstalled(checker.GetInstallationTree(type, version));
}


static async Task TestTasksExecution(PluginManager pluginManager)
{
    var movinput = FileWithFormat.FromFile("/home/i3ym/Downloads/Telegram Desktop/61dc19f37af4207cb6fb6ebb.mov");
    var jpginput = FileWithFormat.FromFile("/home/i3ym/Downloads/Telegram Desktop/2022-05-29 13-06-22.JPG");

    await TestTaskExecution(pluginManager, new GenerateQSPreview(), new QSPreviewInfo(), new(new[] { movinput, jpginput }));
    await TestTaskExecution(pluginManager, new EditVideo(), new EditVideoInfo() { CutFramesAt = new double[] { 1, 2, 3, 4 } }, new(new[] { movinput }));
    await TestTaskExecution(pluginManager, new EsrganUpscale(), new EsrganUpscaleInfo() { X2 = true }, new(new[] { jpginput }));
}


// task itself validates input&output files so we don't need to do much here
static async Task TestTaskExecution<T>(PluginManager pluginManager, PluginAction<T> action, T data, ReadOnlyTaskFileList input)
{
    var plugins = await pluginManager.GetInstalledPluginsAsync();

    var output = new TaskFileListList(TempDirFor(action.Name.ToString()));
    await action.Execute(new TestContext(plugins), new TaskFiles(input, output), data);

    new TestContext(plugins).LogInfo($"!!!                                  {action.GetType().Name} execution completed; Result: [ {string.Join(", ", output.Select(o => $"[{string.Join(", ", o)}]"))}]");
}

static string TempDirFor(string action)
{
    var dir = Path.GetFullPath("temp/" + action);
    if (Directory.Exists(dir)) Directory.Delete(dir, true);
    Directory.CreateDirectory(dir);

    return dir;
}



record TestContext(IReadOnlyCollection<Plugin> Plugins) : ITaskExecutionContext
{
    public IMPlusApi? MPlusApi => null;
    static readonly Logger Logger = LogManager.GetLogger("Testing");

    public void Log(LogLevel level, string text) => Logger.Log(level, text);
    public void SetProgress(double progress) => Log(LogLevel.Info, $"Task progress: {(int) (progress * 100)}%");
}

record InstalledPluginsProvider(IReadOnlyCollection<Plugin> Plugins) : IInstalledPluginsProvider;
record SoftwareListProvider(IReadOnlyDictionary<string, SoftwareDefinition> Software) : ISoftwareListProvider;