using Node.Tasks.Exec.Input;

namespace Node.Tests;

public static class GenericTasksTests
{
    public static async Task RunAsync()
    {
        if (Directory.Exists("/temp/tt"))
            Directory.Delete("/temp/tt", true);

        /*
        await ExecuteSingle(
            ctx => ctx.Resolve<EditVideo>(),
            new TaskFileInput(new ReadOnlyTaskFileList(new[] { FileWithFormat.FromFile("/home/i3ym/Downloads/Telegram Desktop/61dc19f37af4207cb6fb6ebb.mov"), }), "/temp/tt"),
            new EditVideoInfo() { Hflip = true }
        );
        */

        await ExecuteMulti(
            new TaskFileInput(new ReadOnlyTaskFileList(new[] { FileWithFormat.FromFile("/home/i3ym/Downloads/Telegram Desktop/61dc19f37af4207cb6fb6ebb.mov"), }), "/temp/tt"),
            new EditVideoInfo() { Hflip = true, }.ToData(),
            new EditVideoInfo() { Vflip = true, }.ToData()
        );
    }

    static JObject ToData(this object data, string? type = null) => JObject.FromObject(data).WithProperty("type", type ?? data.GetType().Name[..^"Info".Length]);


    static ContainerBuilder CreateTaskBuilder()
    {
        var builder = Container.CreateBuilder();

        builder.RegisterType<ConsoleProgressSetter>()
            .As<IProgressSetter>()
            .SingleInstance();

        builder.RegisterType<GTaskExecutor>()
            .SingleInstance();

        builder.RegisterInstance(new PluginManager(PluginDiscoverers.GetAll()))
            .SingleInstance();

        builder.Register(ctx => new PluginList(ctx.Resolve<PluginManager>().GetInstalledPluginsAsync().GetAwaiter().GetResult()))
            .SingleInstance();

        builder.RegisterType<EditVideo>()
            .Keyed<IGPluginAction>(TaskAction.EditVideo);

        return builder;
    }


    public static async Task<TOutput> ExecuteSingle<TInput, TOutput, TData>(Func<IComponentContext, IGPluginAction<TInput, TOutput, TData>> actionresolvefunc, TInput input, TData data)
        where TInput : notnull
        where TOutput : notnull
        where TData : notnull
    {
        using var container = CreateTaskBuilder().Build();
        return await actionresolvefunc(container).Execute(input, data);
    }

    public static async Task<IReadOnlyList<object>> ExecuteMulti(object input, params JObject[] datas)
    {
        using var container = CreateTaskBuilder().Build();
        return await container.Resolve<GTaskExecutor>()
            .Execute(input, datas);
    }


    class ConsoleProgressSetter : IProgressSetter
    {
        public required ILogger<ConsoleProgressSetter> Logger { get; init; }

        public void Set(double progress) => Logger.LogInformation("Task progress: " + progress);
    }
}
