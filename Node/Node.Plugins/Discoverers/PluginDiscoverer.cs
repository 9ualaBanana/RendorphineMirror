﻿using System.Diagnostics;
using System.Text.RegularExpressions;

namespace Node.Plugins.Discoverers;

internal abstract class PluginDiscoverer : IPluginDiscoverer
{
    public static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    protected IEnumerable<string> InstallationPaths => _installationPaths ??=
        InstallationPathsImpl.Select(Path.TrimEndingDirectorySeparator);
    IEnumerable<string>? _installationPaths;
    protected abstract IEnumerable<string> InstallationPathsImpl { get; }
    protected abstract PluginType PluginType { get; }

    protected virtual string ParentDirectoryPattern => "*";
    protected virtual string ExecutableName => "*";
    protected virtual string? ParentDirectoryRegex => null;
    protected virtual string? ExecutableRegex => null;
    readonly Regex? RegexParentDirectory, RegexExecutable;

    protected virtual bool AllowExeOnLinux => false;

    public PluginDiscoverer()
    {
        RegexParentDirectory = ParentDirectoryRegex is null ? null : new Regex(ParentDirectoryRegex, RegexOptions.Compiled);
        RegexExecutable = ExecutableRegex is null ? null : new Regex($"^{ExecutableRegex}$", RegexOptions.Compiled);
    }

    public Task<IEnumerable<Plugin>> DiscoverAsync() => Task.FromResult(GetPluginsInDirectories(GetPossiblePluginDirectories()));
    protected virtual IEnumerable<string> GetPossiblePluginDirectories()
    {
        var directories = InstallationPaths
            .Where(Directory.Exists)
            .SelectMany(installationPath =>
                Directory.EnumerateDirectories(
                    installationPath,
                    ParentDirectoryPattern,
                    SearchOption.TopDirectoryOnly
                )
                .Append(installationPath)
                .Where(dir => RegexParentDirectory?.IsMatch(Path.GetFileName(dir)!) ?? true)
            );

        if (Environment.OSVersion.Platform == PlatformID.Unix)
            directories = directories.Prepend("/bin");


        return directories;
    }
    protected virtual IEnumerable<Plugin> GetPluginsInDirectories(IEnumerable<string> directories)
    {
        return directories
            .SelectMany(pluginDirectory =>
                Directory.EnumerateFiles(
                    pluginDirectory,
                    ExecutableName,
                    SearchOption.TopDirectoryOnly
                )
                .Where(file => RegexExecutable?.IsMatch(Path.GetFileName(file)) ?? true)
            )
            .Where(path => Environment.OSVersion.Platform == PlatformID.Win32NT ? true : AllowExeOnLinux || !path.EndsWith(".exe"))
            .Select(GetDiscoveredPlugin)
            // skip same versions unless it's unknown
            .DistinctBy(plugin => plugin.Version == "Unknown" ? Guid.NewGuid().ToString() : plugin.Version);
    }

    Plugin GetDiscoveredPlugin(string executablePath) => new Plugin(PluginType, DetermineVersion(executablePath), Path.GetFullPath(executablePath));
    protected virtual string DetermineVersion(string exepath) => "Unknown";


    protected static string StartProcess(string path, string args)
    {
        var proc = Process.Start(new ProcessStartInfo(path, args) { RedirectStandardOutput = true, CreateNoWindow = true })!;
        proc.WaitForExit();

        using var reader = proc.StandardOutput;
        return reader.ReadToEnd();
    }
}