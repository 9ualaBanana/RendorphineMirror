namespace Node.Plugins.Discoverers;

internal class GitPluginDiscoverer : IPluginDiscoverer
{
    public required ILogger<GitPluginDiscoverer> Logger { get; init; }

    public async Task<IEnumerable<Plugin>> DiscoverAsync()
    {
        var gitexe = ProcessLauncher.FindInPath("git", Logger);
        if (!gitexe) return Enumerable.Empty<Plugin>();

        // git version 2.41.0
        var version = await new ProcessLauncher(gitexe.Value, "--version")
            .ExecuteFullAsync();

        version = version.Substring("git version ".Length);
        return new[] { new Plugin(PluginType.Git, version, gitexe.Value) };
    }
}
