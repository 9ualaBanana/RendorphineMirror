using Common.Plugins;

namespace Node.Plugins.Discoverers;

public class FFmpegPluginDiscoverer : PluginDiscoverer
{
    protected override IEnumerable<string> InstallationPathsImpl => new string[]
    {
        @"assets/",
    };
    protected override string ExecutableName => "ffmpeg*";
    protected override PluginType PluginType => PluginType.FFmpeg;

    protected override string DetermineVersion(string exepath)
    {
        var data = StartProcess(exepath, "-version").AsSpan();

        // ffmpeg version n5.0.1 Copyright (c) 2000-2022 the FFmpeg developers
        data = data.Slice("ffmpeg version ".Length);
        data = data.Slice(0, data.IndexOf(' '));

        return new string(data.Trim());
    }
}