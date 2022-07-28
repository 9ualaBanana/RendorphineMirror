using System.Diagnostics;

namespace Machine.Plugins.Installers;

internal record PythonDeploymentInfo : PluginDeploymentInfo
{
    internal PythonDeploymentInfo(string? installationPath = default) : base(installationPath)
    {
    }

    internal override string DownloadUrl => "https://www.python.org/ftp/python/3.10.5/python-3.10.5-amd64.exe";

    internal override ProcessStartInfo InstallationStartInfo
    {
        get
        {
            var installationStartInfo = new ProcessStartInfo
            {
                FileName = InstallerPath,
                CreateNoWindow = true,
                ErrorDialog = false,
            };
            installationStartInfo.ArgumentList.Add("/quiet");
            installationStartInfo.ArgumentList.Add("PrependPath=1");
            if (InstallationPath is not null)
                installationStartInfo.ArgumentList.Add($"TargetDir={InstallationPath}");

            return installationStartInfo;
        }
    }
}
