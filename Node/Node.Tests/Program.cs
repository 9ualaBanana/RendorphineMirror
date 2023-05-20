using System.Diagnostics;
using Node.Plugins;
using Node.Plugins.Models;
using Node.Tasks;
using Node.Tasks.Exec;
using Node.Tasks.Exec.Actions;
using Node.Tasks.Models;

/*
                        actual testing stuff will come later
*/

Initializer.AppName = "renderfin";
Init.Initialize();

PluginsManager.RegisterPluginDiscoverers(PluginDiscoverers.GetAll());
await PluginsManager.DiscoverInstalledPluginsAsync();


await TestTasksExecution();

Debugger.Break();


static async Task TestTasksExecution()
{
    var movinput = FileWithFormat.FromFile("/home/i3ym/Downloads/Telegram Desktop/61dc19f37af4207cb6fb6ebb.mov");
    var jpginput = FileWithFormat.FromFile("/home/i3ym/Downloads/Telegram Desktop/2022-05-29 13-06-22.JPG");

    await TestTaskExecution(new GenerateQSPreview(), new QSPreviewInfo(), new(new[] { movinput, jpginput }));
    await TestTaskExecution(new EditVideo(), new EditVideoInfo() { CutFramesAt = new double[] { 1, 2, 3, 4 } }, new(new[] { movinput }));
    await TestTaskExecution(new EsrganUpscale(), new UpscaleEsrganInfo() { X2 = true }, new(new[] { jpginput }));
}


// task itself validates input&output files so we don't need to do much here
static async Task TestTaskExecution<T>(PluginAction<T> action, T data, ReadOnlyTaskFileList input)
{
    var dir = Path.GetFullPath("temp/" + action.GetType().Name);
    if (Directory.Exists(dir)) Directory.Delete(dir, true);
    Directory.CreateDirectory(dir);

    var output = new TaskFileListList(dir);
    await action.Execute(new TestContext(), new TaskFiles(input, output), data);

    new TestContext().LogInfo($"!!!                                  {action.GetType().Name} execution completed; Result: [ {string.Join(", ", output.Select(o => $"[{string.Join(", ", o)}]"))}]");
}



class TestContext : ITaskExecutionContext
{
    static readonly Logger Logger = LogManager.GetLogger("Testing");
    public IReadOnlyCollection<Plugin> Plugins => PluginsManager.GetInstalledPluginsCache().ThrowIfNull();

    public void Log(LogLevel level, string text) => Logger.Log(level, text);
    public void SetProgress(double progress) => Log(LogLevel.Info, $"Task progress: {(int) (progress * 100)}%");
}