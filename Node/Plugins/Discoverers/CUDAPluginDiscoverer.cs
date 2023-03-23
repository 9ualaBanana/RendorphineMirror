using System.Xml;

namespace Node.Plugins.Discoverers;

public class CUDAPluginDiscoverer : IPluginDiscoverer
{
    public async ValueTask<IEnumerable<Plugin>> Discover()
    {
        var nvcc = Processes.FindFileInPath("nvcc").ThrowIfError();

        var nvidiasmi = await Processes.FullExecute("nvidia-smi", "-q -x", PluginDiscoverer.Logger.AsLoggable(), LogLevel.Off);
        var xml = new XmlDocument().With(xml => xml.LoadXml(nvidiasmi));
        var cudaver = (xml["nvidia_smi_log"]?["cuda_version"]?.FirstChild?.Value).ThrowIfNull();

        return new[] { new Plugin(PluginType.CUDA, cudaver, nvcc) };
    }
}