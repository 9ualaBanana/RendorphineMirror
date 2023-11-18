namespace Node.Tests;

[TestFixture]
public class PluginDeployerTests
{
    /*[Test]
    public async Task DeployEsrganIfNeeded()
    {
        var software = await Apis.DefaultWithSessionId(Consts.SessionId).GetSoftwareAsync().ThrowIfError();
        var manager = new PluginManager(PluginDiscoverers.GetAll());
        var deployer = new PluginDeployer(new Bindable<ImmutableDictionary<string, SoftwareDefinition>>(software));

        var installed = await manager.GetInstalledPluginsAsync();
        await deployer.DeployIfUninstalledAsync(new PluginToDeploy(PluginType.Esrgan.ToString(), ""), installed);
    }

    [Test]
    public async Task DeployEsrgan()
    {
        var software = await Apis.DefaultWithSessionId(Consts.SessionId).GetSoftwareAsync().ThrowIfError();
        var manager = new PluginManager(PluginDiscoverers.GetAll());
        var deployer = new PluginDeployer(new Bindable<ImmutableDictionary<string, SoftwareDefinition>>(software));

        await deployer.ForceDeployAsync(new PluginToDeploy(PluginType.Esrgan.ToString(), ""));

        var installed = await manager.GetInstalledPluginsAsync();
        installed.Should().Contain(p => p.Type == PluginType.Esrgan && p.Version == software[PluginType.Esrgan.ToString()].Versions.Keys.MaxBy(PluginVersion.Parse));
    }*/

    [Test]
    public void TestPluginChecker()
    {
        var software = new Dictionary<PluginType, ImmutableDictionary<PluginVersion, SoftwareVersionInfo>>()
        {
            [PluginType.Esrgan] = new Dictionary<PluginVersion, SoftwareVersionInfo>()
            {
                ["1.0.0"] = new SoftwareVersionInfo(
                    PluginType.Esrgan,
                    "1.0.0",
                    "ESRGAN",
                    null,
                    null,
                    new SoftwareVersionInfo.RequirementsInfo(
                        ImmutableArray<string>.Empty,
                        ImmutableArray<SoftwareVersionInfo.RequirementsInfo.ParentInfo>.Empty
                            .Add(new(PluginType.NvidiaDriver, null))
                    )
                ),
                ["1.0.1"] = new SoftwareVersionInfo(
                    PluginType.Esrgan,
                    "1.0.1",
                    "ESRGAN",
                    null,
                    null,
                    new SoftwareVersionInfo.RequirementsInfo(
                        ImmutableArray<string>.Empty,
                        ImmutableArray<SoftwareVersionInfo.RequirementsInfo.ParentInfo>.Empty
                            .Add(new(PluginType.NvidiaDriver, null))
                    )
                ),
            }.ToImmutableDictionary(),
            [PluginType.Blender] = new Dictionary<PluginVersion, SoftwareVersionInfo>()
            {
                ["1.0.0"] = new SoftwareVersionInfo(
                    PluginType.Blender,
                    "1.0.0",
                    "Blender",
                    null,
                    null,
                    new SoftwareVersionInfo.RequirementsInfo(
                        ImmutableArray<string>.Empty,
                        ImmutableArray<SoftwareVersionInfo.RequirementsInfo.ParentInfo>.Empty
                    )
                ),
            }.ToImmutableDictionary(),
        }.ToImmutableDictionary();


        var installedEsrgan = ImmutableArray<Plugin>.Empty
            .Add(new Plugin(PluginType.Esrgan, "1.0.0", ""));
        var installedNvidia = ImmutableArray<Plugin>.Empty
            .Add(new Plugin(PluginType.NvidiaDriver, "530.41.03", ""));

        gettree(PluginType.Esrgan, "1.0.0", installedEsrgan)
            .ToArray().Should()
            .HaveCount(1, "NvidiaDriver is a parent of ESRGAN but not installed")
            .And.Contain(p => p.Type == PluginType.NvidiaDriver, "NvidiaDriver is a parent of ESRGAN but not installed");

        FluentActions.Enumerating(() => gettree(PluginType.Esrgan, "1.2.3", installedNvidia))
            .Should().Throw<Exception>("registry does not have ESRGAN version 1.2.3");


        gettree(PluginType.Esrgan, "1.0.0", installedNvidia)
            .ToArray().Should()
            .HaveCount(1)
            .And.ContainSingle(p => p.Type == PluginType.Esrgan && p.Version == "1.0.0");

        gettree(PluginType.Esrgan, null, installedNvidia)
            .ToArray().Should()
            .HaveCount(1)
            .And.ContainSingle(p => p.Type == PluginType.Esrgan && p.Version == "1.0.1");


        var installed = ImmutableArray<Plugin>.Empty
            .Add(new Plugin(PluginType.Esrgan, "1.0.1", ""))
            .Add(new Plugin(PluginType.NvidiaDriver, "530.41.03", ""));

        gettree(PluginType.Esrgan, "1.0.0", installed)
            .ToArray().Should()
            .HaveCount(1)
            .And.ContainSingle(p => p.Type == PluginType.Esrgan && p.Version == "1.0.0");

        gettree(PluginType.Esrgan, "1.0.1", installed)
            .ToArray().Should()
            .BeEmpty("version 1.0.1 is already installed");

        gettree(PluginType.Esrgan, null, installed)
            .ToArray().Should()
            .BeEmpty("latest version requested (1.0.1) and already installed");



        IEnumerable<PluginToInstall> gettree(PluginType type, string? version, IReadOnlyCollection<Plugin> installed) =>
            PluginChecker.GetInstallationTree(software, type, new PluginVersion(version))
                .Where(p => !PluginDeployer.IsInstalled(installed, p.Type, p.Version));
    }
}
