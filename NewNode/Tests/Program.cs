using Node.Plugins;
using Node.Plugins.Models;
using Node.Tasks;
using Node.Tasks.Exec.Actions;
using Node.Tasks.Models;

Initializer.AppName = "renderfin";

PluginsManager.RegisterPluginDiscoverers(PluginDiscoverers.GetAll());
await PluginsManager.DiscoverInstalledPluginsAsync();



await new EsrganUpscale().Execute(new TestContext(),
    new TaskFiles(
        new ReadOnlyTaskFileList(new[]
        {
            new FileWithFormat(FileFormat.Jpeg, "/home/i3ym/Загрузки/Telegram Desktop/2022-05-29 13-06-22.JPG")
        }),
        new TaskFileListList("/temp/asd")
    ),
    new UpscaleEsrganInfo()
);



class TestContext : ITaskExecutionContext
{
    public IReadOnlyCollection<Plugin> Plugins => PluginsManager.GetInstalledPluginsCache().ThrowIfNull();

    public void Log(LogLevel level, string text) =>
        Console.WriteLine($"[{level} : Testing] {text}");

    public void SetProgress(double progress) =>
        Log(LogLevel.Info, $"Task progress: {(int) (progress * 100)}%");
}