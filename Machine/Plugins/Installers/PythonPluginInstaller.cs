using System.Diagnostics;

namespace Machine.Plugins.Installers;

public class PythonPluginInstaller : PluginInstaller
{
    protected override string DownloadUrl => "https://www.python.org/ftp/python/3.10.5/python-3.10.5-amd64.exe";

    public PythonPluginInstaller(HttpClient httpClient, CancellationToken cancellationToken = default)
        : base(httpClient, cancellationToken)
    {
    }

    protected override ProcessStartInfo GetInstallationInfo(string installerPath) => new()
    {
        FileName = installerPath,
        Arguments = "/quiet PrependPath=1",
        CreateNoWindow = true,
        ErrorDialog = false,
    };
}
