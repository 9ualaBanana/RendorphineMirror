namespace Machine.Plugins.Plugins;

internal record PythonPlugin : Plugin
{
    public PythonPlugin(string path) : base(path)
    {
    }

    public override PluginType Type => PluginType.Python;

    protected override string DetermineVersion()
    {
        string version = string.Join(
            null, Directory.GetParent(Path)!.Name.SkipWhile(c => !char.IsDigit(c))
            );
        return version.Contains('.') ? version : Format(version);
    }

    static string Format(string version) =>
        version[..1] + '.' + string.Join(null, version[1..].TakeWhile(char.IsDigit));
}
