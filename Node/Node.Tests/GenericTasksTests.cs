using Node.Tasks.Exec.Input;

namespace Node.Tests;

public static partial class GenericTasksTests
{
    public static async Task RunAsync()
    {
        if (Directory.Exists("/temp/tt"))
            Directory.Delete("/temp/tt", true);

        await ExecuteSingle(
            new EditVideo(),
            new TaskFileInput(new ReadOnlyTaskFileList(new[] { FileWithFormat.FromFile("/home/i3ym/Downloads/Telegram Desktop/61dc19f37af4207cb6fb6ebb.mov"), }), "/temp/tt"),
            new EditVideoInfo() { Hflip = true }
        );

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

        builder.RegisterType<TaskExecutorByData>()
            .SingleInstance();

        builder.RegisterInstance(new PluginManager(PluginDiscoverers.GetAll()))
            .SingleInstance();

        builder.Register(ctx => new PluginList(ctx.Resolve<PluginManager>().GetInstalledPluginsAsync().GetAwaiter().GetResult()))
            .SingleInstance();

        builder.RegisterType<EditVideo>()
            .Keyed<IPluginActionInfo>(TaskAction.EditVideo);

        return builder;
    }


    public static async Task<TOutput> ExecuteSingle<TInput, TOutput, TData>(PluginActionInfo<TInput, TOutput, TData> action, TInput input, TData data)
        where TInput : notnull
        where TOutput : notnull
        where TData : notnull
    {
        using var container = CreateTaskBuilder().Build();
        return await action.Execute(container, input, data);
    }

    public static async Task<IReadOnlyList<object>> ExecuteMulti(object input, params JObject[] datas)
    {
        using var container = CreateTaskBuilder().Build();
        return await container.Resolve<TaskExecutorByData>()
            .Execute(input, datas);
    }
}
