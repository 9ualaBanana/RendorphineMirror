using System.Diagnostics;

namespace Machine.Plugins.Installers;

public class PythonPluginInstaller : PluginInstaller
{
    protected override string DownloadUrl => "https://www.python.org/ftp/python/3.10.5/python-3.10.5-amd64.exe";

    public PythonPluginInstaller(HttpClient httpClient, CancellationToken cancellationToken = default)
        : base(httpClient, cancellationToken)
    {
    }

    protected override ProcessStartInfo GetInstallationInfo(
        string installerPath, string? installationPath = default)
    {
        var installationInfo = new ProcessStartInfo
        {
            FileName = installerPath,
            CreateNoWindow = true,
            ErrorDialog = false,
        };
        installationInfo.ArgumentList.Add("/quiet");
        installationInfo.ArgumentList.Add("PrependPath=1");
        if (installationPath is not null)
            installationInfo.ArgumentList.Add($"TargetDir={installationPath}");

        return installationInfo;
    }
}
