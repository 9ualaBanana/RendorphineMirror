using System.Diagnostics;

namespace Common.Plugins.Deployment;

public record PythonDeploymentInfo : DownloadablePluginDeploymentInfo
{
    public PythonDeploymentInfo(string? installationPath = default) : base(installationPath)
    {
    }

    public override string DownloadUrl => "https://www.python.org/ftp/python/3.10.5/python-3.10.5-amd64.exe";

    public override Func<CancellationToken, Task>? Installation
    {
        get => async (cancellationToken) =>
        {
            using var installation = Process.Start(_InstallationStartInfo)!;
            await installation.WaitForExitAsync(cancellationToken);
        };
    }

    ProcessStartInfo _InstallationStartInfo
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
