namespace Machine.Plugins.Plugins;

internal record PythonPlugin : Plugin
{
    public PythonPlugin(string path) : base(path)
    {
    }

    public override PluginType Type => PluginType.Python;

    protected override string DetermineVersion()
    {
        var data = StartProcess("--version").AsSpan();

        // Python 3.10.6
        data = data.Slice("Python ".Length);

        return new string(data.Trim());
    }

    static string Format(string version) =>
        version[..1] + '.' + string.Join(null, version[1..].TakeWhile(char.IsDigit));
}
