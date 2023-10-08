using Node.Tasks.Exec.Input;

namespace Node.Tests;

public static partial class GenericTasksTests
{
    public static async Task RunAsync(ILifetimeScope container)
    {
        if (Directory.Exists("/temp/tt"))
            Directory.Delete("/temp/tt", true);

        await ExecuteSingle(
            container,
            new EditVideo(),
            new TaskFileInput(new ReadOnlyTaskFileList(new[] { FileWithFormat.FromFile("/home/i3ym/Downloads/Telegram Desktop/61dc19f37af4207cb6fb6ebb.mov"), }), "/temp/tt"),
            new EditVideoInfo() { Hflip = true }
        );

        await ExecuteMulti(
            container,
            new TaskFileInput(new ReadOnlyTaskFileList(new[] { FileWithFormat.FromFile("/home/i3ym/Downloads/Telegram Desktop/61dc19f37af4207cb6fb6ebb.mov"), }), "/temp/tt"),
            new EditVideoInfo() { Hflip = true, }.ToData(),
            new EditVideoInfo() { Vflip = true, }.ToData()
        );
    }

    static JObject ToData(this object data, string? type = null) => JObject.FromObject(data).WithProperty("type", type ?? data.GetType().Name[..^"Info".Length]);


    static void InitializeForTasks(ContainerBuilder builder)
    {
        builder.RegisterType<ConsoleProgressSetter>()
            .AsImplementedInterfaces()
            .SingleInstance();

        builder.RegisterType<TaskExecutorByData>()
            .SingleInstance();

        PluginDiscoverers.RegisterDiscoverers(builder);
        builder.RegisterType<PluginManager>()
            .AsSelf()
            .As<IInstalledPluginsProvider>()
            .SingleInstance();

        builder.Register(ctx => new PluginList(ctx.Resolve<PluginManager>().GetInstalledPluginsAsync().GetAwaiter().GetResult()))
            .SingleInstance();

        builder.RegisterType<EditVideo>()
            .Keyed<IPluginActionInfo>(TaskAction.EditVideo);
    }


    public static async Task<TOutput> ExecuteSingle<TInput, TOutput, TData>(ILifetimeScope container, PluginActionInfo<TInput, TOutput, TData> action, TInput input, TData data)
        where TInput : notnull
        where TOutput : notnull
        where TData : notnull
    {
        using var ctx = container.BeginLifetimeScope(InitializeForTasks);
        return await action.Execute(ctx, input, data);
    }

    public static async Task<object> ExecuteMulti(ILifetimeScope container, object input, params JObject[] datas)
    {
        using var ctx = container.BeginLifetimeScope(InitializeForTasks);
        return await ctx.Resolve<TaskExecutorByData>()
            .Execute(input, datas);
    }
}
