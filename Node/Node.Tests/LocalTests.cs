using Node.Common;
using Node.Tasks.Exec.Actions;
using Node.Tasks.Exec.Input;
using NodeToUI;
using static Node.Tests.GenericTasksTests;

namespace Node.Tests;

public class LocalTests
{
    public required DataDirs Dirs { get; init; }
    public required ILifetimeScope Context { get; init; }
    public required ILogger<LocalTests> Logger { get; init; }

    public async Task Run()
    {
        Logger.LogInformation("Running tests...");

        // await ElevenLabsTest();
        await LaunchTask();
        //await PluginTest();
        //await GenericTasksTests.RunAsync(Context);
    }

    async Task LaunchQSPreview(ILifetimeScope container)
    {
        using var ctx = container.BeginLifetimeScope(builder =>
        {
            builder.RegisterType<ConsoleProgressSetter>()
                .AsImplementedInterfaces()
                .SingleInstance();
        });

        var result = await ctx.Resolve<GenerateQSPreview>()
            .Execute(
                ctx,
                new TaskFileInput(new ReadOnlyTaskFileList(new[] { FileWithFormat.FromFile(@"C:\mp4.mp4"), FileWithFormat.FromFile(@"C:\png.png") }), @"c:\resultdir\"),
                new QSPreviewInfo("qwertystockfileid")
            );
    }

    async Task LaunchTask()
    {
        await ExecuteSingle(
            Context,
            new EditRaster(),
            new TaskFileInput(new ReadOnlyTaskFileList(new[] { FileWithFormat.FromFile("/home/i3ym/workspace/workdir/testvideo/landscape.jpg"), }), "/temp/tt"),
            new EditRasterInfo() { Scale = new() { W = 512, H = 512 } }
        );

        await ExecuteSingle(
            Context,
            new EditVideo(),
            new TaskFileInput(new ReadOnlyTaskFileList(new[] { FileWithFormat.FromFile("/home/i3ym/workspace/workdir/testvideo/61dc19f37af4207cb6fb6ebb.mov"), }), "/temp/tt"),
            new EditVideoInfo() { Scale = new() { W = 512, H = 512 } }
        );
    }

    async Task ElevenLabsTest()
    {
        using var ctx = Context.BeginLifetimeScope(builder =>
        {
            builder.RegisterType<HttpClient>()
                .SingleInstance();
            builder.RegisterType<ElevenLabsApi>()
                .SingleInstance();
            builder.RegisterType<ElevenLabsApis>()
                .WithParameter("apiKey", File.ReadAllText("elevenlabsapikey").Trim())
                .SingleInstance();
        });

        var api = ctx.Resolve<ElevenLabsApis>();


        var voices = await api.GetVoiceListAsync()
            .ThrowIfError();

        var ttsfile = Dirs.NamedTempFile("eleven.mp3");
        await api.TextToSpeechAsync(voices[0].VoiceId, "eleven_monolingual_v1", "Hello I am the bot from the api test, wow!", ttsfile)
            .ThrowIfError();
    }

    /// <summary> Test installation and deletion of plugins, with conda environments </summary>
    async Task PluginTest()
    {
        using var ctx = Context.BeginLifetimeScope(builder =>
        {
            builder.RegisterType<HttpClient>()
                .SingleInstance();
            builder.RegisterType<Api>()
                .SingleInstance();

            builder.RegisterType<Updaters.SoftwareUpdater>()
                .SingleInstance();

            builder.RegisterType<TorrentClient>()
                .WithParameter("dhtport", 39999)
                .WithParameter("listenport", 39998)
                .SingleInstance();


            PluginDiscoverers.RegisterDiscoverers(builder);

            builder.RegisterInstance(new PluginDirs(Directories.NewDirCreated("temp/plugins")))
                .SingleInstance();

            builder.RegisterType<PluginManager>()
                .AsSelf()
                .As<IPluginList>()
                .SingleInstance();

            builder.RegisterType<PowerShellInvoker>()
                .SingleInstance();
            builder.RegisterType<CondaInvoker>()
                .SingleInstance();
            builder.RegisterType<CondaManager>()
                .SingleInstance();

            builder.RegisterType<PluginDeployer>()
                .SingleInstance();
        });

        // returns only plugins installed in PluginDirs.Directory
        IEnumerable<Plugin> filterLocalPlugins(IEnumerable<Plugin> plugins) => plugins.Where(p => p.Path.StartsWith(ctx.Resolve<PluginDirs>().Directory, StringComparison.Ordinal));

        var conda = ctx.Resolve<CondaManager>();
        var condainvoker = ctx.Resolve<CondaInvoker>();
        var manager = ctx.Resolve<PluginManager>();
        var deployer = ctx.Resolve<PluginDeployer>();
        var software = await ctx.Resolve<Updaters.SoftwareUpdater>().Update().ThrowIfError();

        var pltype = PluginType.ImageDetector;
        var plver = software[pltype].Values.MaxBy(v => v.Version).ThrowIfNull().Version.ToString();
        var condaenv = PluginDeployer.GetCondaEnvName(pltype, plver);


        await checkLocalInstalledPlugins();
        async Task checkLocalInstalledPlugins()
        {
            var installed = filterLocalPlugins(await manager.RediscoverPluginsAsync());
            installed.Should()
                .BeEmpty();
        }


        await install();
        async Task install()
        {
            var tree = PluginChecker.GetInstallationTree(software, pltype, plver);
            (await deployer.DeployUninstalled(tree, default)).Should()
                .Be(1);

            var installed = filterLocalPlugins(await manager.RediscoverPluginsAsync());
            installed.Should()
                .HaveCount(1)
                .And.BeEquivalentTo(new[] { new Plugin(pltype, plver, Path.GetFullPath(Path.Combine(ctx.Resolve<PluginDirs>().Directory, pltype.ToString().ToLowerInvariant(), plver.ToString(), "main.py"))), });

            conda.EnvironmentExists(condaenv).Should()
                .BeTrue();
        }


        await restoreDeletedCondaEnvironment();
        async Task restoreDeletedCondaEnvironment()
        {
            conda.DeleteEnvironment(condaenv);
            conda.EnvironmentExists(condaenv).Should()
                .BeFalse();

            new Action(() => condainvoker.ExecutePowerShellAtWithCondaEnv(ctx.Resolve<IPluginList>(), pltype, "echo test", delegate { })).Should()
                .NotThrow();

            conda.EnvironmentExists(condaenv).Should()
                .BeTrue();
        }


        await delete();
        async Task delete()
        {
            await deployer.Delete(pltype, plver, false);

            var installed = filterLocalPlugins(await manager.RediscoverPluginsAsync());
            installed.Should()
                .BeEmpty();

            conda.EnvironmentExists(condaenv).Should()
                .BeFalse();
        }
    }
}
