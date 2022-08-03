namespace Machine.Plugins.Plugins;

public record PythonEsrganPlugin : Plugin
{
    public PythonEsrganPlugin(string path) : base(path)
    {
    }

    public override PluginType Type => PluginType.Python_Esrgan;
}
