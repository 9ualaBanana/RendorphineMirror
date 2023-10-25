using System.Management.Automation;
using MonoTorrent;
using MonoTorrent.Client;

namespace Node.Plugins;

public class PluginDeployer
{
    public required PowerShellInvoker PowerShellInvoker { get; init; }
    public required IInstalledPluginsProvider InstalledPlugins { get; init; }
    public required CondaManager CondaManager { get; init; }
    public required TorrentClient TorrentClient { get; init; }
    public required HttpClient HttpClient { get; init; }
    public required PluginDirs PluginDirs { get; init; }
    public required ILogger<PluginDeployer> Logger { get; init; }

    public static string GetCondaEnvName(PluginType type, PluginVersion version) => $"{type.ToString().ToLowerInvariant()}_{version}";


    /// <param name="version"> Plugin version or null if any </param>
    public bool IsInstalled(PluginType type, PluginVersion version) => IsInstalled(InstalledPlugins.Plugins, type, version);

    /// <inheritdoc cref="IsInstalled(PluginType, PluginVersion)"/>
    public static bool IsInstalled(IReadOnlyCollection<Plugin> installed, PluginType type, PluginVersion version) =>
        installed.Any(i => i.Type == type && (version.IsEmpty || i.Version == version));


    /// <remarks>
    /// Deploys only specified plugins;
    /// To get plugin list with parents, use <see cref="PluginChecker.GetInstallationTree(ImmutableDictionary{string, ImmutableDictionary{PluginVersion, SoftwareVersionInfo}}, IEnumerable{PluginToDeploy})"/>.
    /// Deploys only non-installed plugins.
    /// </remarks>
    /// <returns> Amount of installed plugins </returns>
    public async Task<int> DeployUninstalled(IEnumerable<PluginToInstall> plugins, CancellationToken token)
    {
        var uninstalled = plugins.Where(plugin => !IsInstalled(plugin.Type, plugin.Version)).ToArray();

        if (uninstalled.Any(p => p.Installation is null))
            throw new Exception($"Plugins {string.Join(", ", uninstalled.Where(p => p.Installation is null))} are not installed and can't be");

        var installed = 0;
        foreach (var plugin in uninstalled)
        {
            await Deploy(plugin.Type, plugin.Version, plugin.Installation.ThrowIfNull($"Plugin {plugin} somehow has null installScript"), token);
            installed++;
        }

        return installed;
    }

    /// <remarks> Deploys even if installed </remarks>
    public async Task Deploy(PluginType type, PluginVersion version, SoftwareVersionInfo.InstallationInfo installation, CancellationToken token)
    {
        using var _logscope = Logger.BeginScope($"Installing {type} {version}");

        Logger.LogInformation($"Installing");

        if (installation.Source is not null)
            await Download(type, version, installation.Source, token);
        if (installation.Script is not null)
            InstallWithPowershell(type, version, installation.Script);
        if (installation.Python is not null)
            InstallWithConda(type, version, installation.Python);

        Logger.LogInformation($"Installed");
    }

    async Task Download(PluginType type, PluginVersion version, SoftwareVersionInfo.InstallationInfo.SourceInfo source, CancellationToken token)
    {
        // clearing old plugin files if any
        var targetdir = Directories.NewDirCreated(PluginDirs.GetPluginDirectory(type, version));

        if (source is SoftwareVersionInfo.InstallationInfo.RegistrySourceInfo)
            await downloadRegistry(token);
        else if (source is SoftwareVersionInfo.InstallationInfo.UrlSourceInfo url)
            await downloadUrl(url.Url, token);


        async Task downloadRegistry(CancellationToken token)
        {
            Logger.LogInformation($"Downloading from registry to {targetdir}");

            // MemoryStream needed as TorrentClient.AddOrGetTorrent requires seeking support
            TorrentManager manager;
            using (var torrentstreamcopy = new MemoryStream())
            using (var torrentresponse = await HttpClient.GetAsync(Api.AppendQuery($"{Apis.RegistryUrl}/soft/gettorrent", ("plugin", type.ToString()), ("version", version.ToString())), token))
            using (var torrentstream = await torrentresponse.Content.ReadAsStreamAsync(token))
            {
                await torrentstream.CopyToAsync(torrentstreamcopy, token);
                await Api.LogRequest(torrentresponse, null, "Getting plugin torrent", Logger, token);
                torrentstreamcopy.Position = 0;

                manager = await TorrentClient.AddOrGetTorrent(await Torrent.LoadAsync(torrentstreamcopy), targetdir);
            }


            await TorrentClient.WaitForCompletion(manager, new TimeoutCancellationToken(token));
        }
        async Task downloadUrl(string url, CancellationToken token)
        {
            var resultfilename = new Uri(url).Segments.Last().Trim('/');
            foreach (var invalid in Path.GetInvalidFileNameChars())
                resultfilename = resultfilename.Replace(invalid, '-');

            var result = Path.Combine(targetdir, resultfilename);
            Logger.LogInformation($"Downloading from {url} to {result}");

            using var inputstream = await HttpClient.GetStreamAsync(url, token);
            using var resultfile = File.Create(Path.Combine(targetdir, result));
            await inputstream.CopyToAsync(inputstream, token);
        }
    }
    void InstallWithPowershell(PluginType type, PluginVersion version, string script)
    {
        if (CondaManager.EnvironmentExists(GetCondaEnvName(type, version)))
        {
            script = $"""
                {CondaManager.GetActivateScript(InstalledPlugins.Plugins.First(p => p.Type == PluginType.Conda).Path, GetCondaEnvName(type, version))}
                {script}
                """;
        }

        var pwsh = PowerShellInvoker.Initialize(script);
        var pldownload = setVars(pwsh);
        var result = PowerShellInvoker.Invoke(pwsh);

        if (Directory.Exists(pldownload))
            Directory.Delete(pldownload, true);


        /// <returns> Plugin download directory </returns>
        string setVars(PowerShell psh)
        {
            var prox = psh.Runspace.SessionStateProxy;
            var plugintype = type.ToString().ToLowerInvariant();
            var pluginver = version.ToString();

            var pluginverpath = pluginver;
            foreach (var invalid in Path.GetInvalidFileNameChars())
                pluginverpath = pluginverpath.Replace(invalid, '_');


            prox.SetVariable("PLUGIN", plugintype);
            prox.SetVariable("PLUGINVER", pluginver);

            prox.SetVariable("PLTORRENT", plugintype + "." + pluginverpath);

            var pldownload = Directories.DirCreated(Path.Combine(prox.GetVariable("DOWNLOADS").ToString()!, plugintype, pluginverpath));
            prox.SetVariable("PLDOWNLOAD", pldownload);
            prox.SetVariable("PLINSTALL", Directories.DirCreated(Path.Combine(PluginDirs.Directory, plugintype, pluginverpath)));

            return pldownload;
        }
    }
    void InstallWithConda(PluginType type, PluginVersion version, SoftwareVersionInfo.InstallationInfo.PythonInfo info)
    {
        var name = GetCondaEnvName(type, version);
        var condapath = InstalledPlugins.Plugins.First(p => p.Type == PluginType.Conda).Path;

        CondaManager.InitializeEnvironment(condapath, name, info.Version, info.Conda.Requirements, info.Conda.Channels, info.Pip.Requirements, info.Pip.RequirementFiles, Directories.DirCreated(PluginDirs.GetPluginDirectory(type, version)));
    }


    public async Task Delete(PluginType type, PluginVersion version)
    {
        var name = GetCondaEnvName(type, version);
        CondaManager.DeleteEnvironment(name);

        var plugindir = PluginDirs.GetPluginDirectory(type, version);
        if (Directory.Exists(plugindir))
            Directory.Delete(plugindir, true);
    }
}
