using Node.Common;
using Node.Plugins;
using Node.Plugins.Models;

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
        var software = ImmutableDictionary<string, SoftwareDefinition>.Empty
            .Add(PluginType.Esrgan.ToString(), new SoftwareDefinition("ESRGAN",
                ImmutableDictionary<PluginVersion, SoftwareVersionDefinition>.Empty
                    .Add("1.0.0", new SoftwareVersionDefinition("echo test", new SoftwareRequirements(
                        ImmutableDictionary<PlatformID, SoftwareSupportedPlatform>.Empty,
                        ImmutableArray<SoftwareParent>.Empty
                            .Add(new SoftwareParent(PluginType.NvidiaDriver.ToString(), ""))
                    )))
                    .Add("1.0.1", new SoftwareVersionDefinition("echo test", new SoftwareRequirements(
                        ImmutableDictionary<PlatformID, SoftwareSupportedPlatform>.Empty,
                        ImmutableArray<SoftwareParent>.Empty
                            .Add(new SoftwareParent(PluginType.NvidiaDriver.ToString(), ""))
                    )))
                )
            )
            .Add(PluginType.Blender.ToString(), new SoftwareDefinition("Blender",
                ImmutableDictionary<PluginVersion, SoftwareVersionDefinition>.Empty
                    .Add("1.0.0", new SoftwareVersionDefinition("echo test", new SoftwareRequirements(ImmutableDictionary<PlatformID, SoftwareSupportedPlatform>.Empty, ImmutableArray<SoftwareParent>.Empty))))
            );

        var deployer = new PluginDeployer2();


        var installedEsrgan = ImmutableArray<Plugin>.Empty
            .Add(new Plugin(PluginType.Esrgan, "1.0.0", ""));
        var installedNvidia = ImmutableArray<Plugin>.Empty
            .Add(new Plugin(PluginType.NvidiaDriver, "530.41.03", ""));

        var t = PluginChecker.GetInstallationTree(PluginType.Esrgan, "1.0.0", software).ToArray();
        var tree = gettree(PluginType.Esrgan, "1.0.0", software, installedEsrgan).ToArray();

        gettree(PluginType.Esrgan, "1.0.0", software, installedEsrgan)
            .ToArray().Should()
            .HaveCount(1, "NvidiaDriver is a parent of ESRGAN but not installed")
            .And.Contain(p => p.Type == PluginType.NvidiaDriver, "NvidiaDriver is a parent of ESRGAN but not installed");

        FluentActions.Enumerating(() => gettree(PluginType.Esrgan, "1.2.3", software, installedNvidia))
            .Should().Throw<Exception>("registry does not have ESRGAN version 1.2.3");


        gettree(PluginType.Esrgan, "1.0.0", software, installedNvidia)
            .ToArray().Should()
            .HaveCount(1)
            .And.ContainSingle(p => p.Type == PluginType.Esrgan && p.Version == "1.0.0");

        gettree(PluginType.Esrgan, null, software, installedNvidia)
            .ToArray().Should()
            .HaveCount(1)
            .And.ContainSingle(p => p.Type == PluginType.Esrgan && p.Version == "1.0.1");


        var installed = ImmutableArray<Plugin>.Empty
            .Add(new Plugin(PluginType.Esrgan, "1.0.1", ""))
            .Add(new Plugin(PluginType.NvidiaDriver, "530.41.03", ""));

        gettree(PluginType.Esrgan, "1.0.0", software, installed)
            .ToArray().Should()
            .HaveCount(1)
            .And.ContainSingle(p => p.Type == PluginType.Esrgan && p.Version == "1.0.0");

        gettree(PluginType.Esrgan, "1.0.1", software, installed)
            .ToArray().Should()
            .BeEmpty("version 1.0.1 is already installed");

        gettree(PluginType.Esrgan, null, software, installed)
            .ToArray().Should()
            .BeEmpty("latest version requested (1.0.1) and already installed");



        static IEnumerable<PluginToInstall> gettree(PluginType type, string? version, IReadOnlyDictionary<string, SoftwareDefinition> software, IReadOnlyCollection<Plugin> installedPlugins) =>
            PluginChecker.GetInstallationTree(type, version, software)
                .Where(p => !PluginChecker.IsInstalled(p.Type, p.Version, installedPlugins));
    }
}
