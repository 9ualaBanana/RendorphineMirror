using Node.Common;
using Node.Tasks.Exec.Actions;
using Node.Tasks.Exec.Input;

namespace Node.Tests;

public static partial class GenericTasksTests
{
    public static async Task RunAsync(ILifetimeScope container)
    {
        if (Directory.Exists("/temp/tt"))
            Directory.Delete("/temp/tt", true);

        var result1 = await ExecuteSingle(
            container,
            new EditVideo(),
            new TaskFileInput(new ReadOnlyTaskFileList(new[] { FileWithFormat.FromFile("/home/i3ym/workspace/workdir/testvideo/61dc19f37af4207cb6fb6ebb.mov"), }), "/temp/tt/1"),
            new EditVideoInfo() { Hflip = true }
        );

        var result2 = (ReadOnlyTaskFileList) await ExecuteMulti(
            container,
            new object[]
            {
                new TaskFileInput(new ReadOnlyTaskFileList(new[] { FileWithFormat.FromFile("/home/i3ym/workspace/workdir/testvideo/61dc19f37af4207cb6fb6ebb.mov"), }), "/temp/tt/2/1"),
                new TaskFileInput(new ReadOnlyTaskFileList(new[] { FileWithFormat.FromFile("/home/i3ym/workspace/workdir/testvideo/62bd65167538331c6b6c6574.mov"), }), "/temp/tt/2/2"),
            },
            new EditVideoInfo() { Hflip = true }.ToData()
        );

        var result3 = (ReadOnlyTaskFileList) await ExecuteMulti(
            container,
            new object[]
            {
                new TaskFileInput(new ReadOnlyTaskFileList(new[] { FileWithFormat.FromFile("/home/i3ym/workspace/workdir/testvideo/61dc19f37af4207cb6fb6ebb.mov"), }), "/temp/tt/3/1"),
                new TaskFileInput(new ReadOnlyTaskFileList(new[] { FileWithFormat.FromFile("/home/i3ym/workspace/workdir/testvideo/62bd65167538331c6b6c6574.mov"), }), "/temp/tt/3/2"),
            },
            new EditVideoInfo() { Hflip = true }.ToData(),
            new EditVideoInfo() { Vflip = true, }.ToData()
        );

        var result4 = (ReadOnlyTaskFileList) await ExecuteMulti(
            container,
            new TaskFileInput(new ReadOnlyTaskFileList(new[] { FileWithFormat.FromFile("/home/i3ym/workspace/workdir/testvideo/61dc19f37af4207cb6fb6ebb.mov"), }), "/temp/tt/4"),
            new EditVideoInfo() { Hflip = true, }.ToData(),
            new EditVideoInfo() { Vflip = true, }.ToData()
        );

        _ = new object[] { result1, result2, result3, result4 };
    }

    static JObject ToData(this object data, string? type = null) => JObject.FromObject(data).WithProperty("type", type ?? data.GetType().Name[..^"Info".Length]);


    static void InitializeForTasks(ContainerBuilder builder)
    {
        builder.RegisterType<ConsoleProgressSetter>()
            .AsImplementedInterfaces()
            .SingleInstance();

        builder.RegisterType<TaskExecutorByData>()
            .SingleInstance();

        builder.RegisterType<CondaManager>()
            .SingleInstance();

        builder.RegisterInstance(new PluginDirs("plugins"))
            .SingleInstance();

        PluginDiscoverers.RegisterDiscoverers(builder);
        builder.RegisterType<PluginManager>()
            .AsSelf()
            .As<IPluginList>()
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

    public static async Task<object> ExecuteMulti(ILifetimeScope container, object input, params JObject[] datas) =>
        await ExecuteMulti(container, new[] { input }, datas);
    public static async Task<object> ExecuteMulti(ILifetimeScope container, IReadOnlyList<object> input, params JObject[] datas)
    {
        using var ctx = container.BeginLifetimeScope(InitializeForTasks);
        return await ctx.Resolve<TaskExecutorByData>()
            .Execute(input, datas);
    }
}
