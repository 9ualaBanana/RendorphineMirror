namespace Node.Plugins.Discoverers;

internal class UnityPluginDiscoverer : PluginDiscoverer
{
    // C:\Program Files\Unity 2020.3.37f1\Editor\Unity.exe
    // C:\Program Files\Unity\Hub\Editor\2022.3.12f1\Editor\Unity.exe
    // Since both paths and dir names could be different, \Unity\Hub\ path is handled by overridden GetPossiblePluginDirectories

    protected override IEnumerable<string> InstallationPathsImpl => new List<string>
    {
        $@"{Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles)}",
        $@"{Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86)}",
    };
    protected override string ParentDirectoryRegex => "Unity .*";
    protected override string ExecutableName => "Unity.exe";
    protected override PluginType PluginType => PluginType.Unity;

    protected override IEnumerable<string> GetPossiblePluginDirectories()
    {
        var unityHubDirs = new[]
        {
            $@"{Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles)}\Unity\Hub\Editor",
            $@"{Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86)}\Unity\Hub\Editor",
        };

        var unityHubInstallations = unityHubDirs
            .Where(Directory.Exists)
            .SelectMany(Directory.GetDirectories);

        var programFilesInstallations = base.GetPossiblePluginDirectories();

        return unityHubInstallations.Concat(programFilesInstallations);
    }

    protected override IEnumerable<Plugin> GetPluginsInDirectories(IEnumerable<string> directories) =>
        base.GetPluginsInDirectories(directories.Select(dir => Path.Combine(dir, "Editor")));

    protected override string DetermineVersion(string exepath)
    {
        return Path.GetDirectoryName(Path.GetDirectoryName(exepath)!)!.Replace("Unity", string.Empty).Trim();
    }
}
