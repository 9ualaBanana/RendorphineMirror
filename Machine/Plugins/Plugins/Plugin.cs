using System.Diagnostics;

namespace Machine.Plugins.Plugins;

public abstract record Plugin
{
    public abstract PluginType Type { get; }
    public string Version => _version ??= DetermineVersion();
    string? _version;
    public string Path { get; }

    internal Plugin(string path)
    {
        Path = path;
    }

    protected virtual string DetermineVersion() => "Unknown";


    protected string StartProcess(string args)
    {
        var proc = Process.Start(new ProcessStartInfo(Path, args) { RedirectStandardOutput = true })!;
        proc.WaitForExit();

        using var reader = proc.StandardOutput;
        return reader.ReadToEnd();
    }
}
