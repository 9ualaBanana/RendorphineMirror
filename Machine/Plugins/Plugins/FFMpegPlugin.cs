namespace Machine.Plugins.Plugins;

internal record FFmpegPlugin : Plugin
{
    public FFmpegPlugin(string path) : base(path) { }

    public override PluginType Type => PluginType.FFmpeg;

    protected override string DetermineVersion()
    {
        var data = StartProcess("-version").AsSpan();

        // ffmpeg version n5.0.1 Copyright (c) 2000-2022 the FFmpeg developers
        data = data.Slice("ffmpeg version ".Length);
        data = data.Slice(0, data.IndexOf(' '));

        return new string(data.Trim());
    }
}