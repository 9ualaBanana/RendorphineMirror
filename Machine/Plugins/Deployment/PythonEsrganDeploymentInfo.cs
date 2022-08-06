using System.IO.Compression;

namespace Machine.Plugins.Deployment;

public record PythonEsrganDeploymentInfo : PluginDeploymentInfo
{
    public PythonEsrganDeploymentInfo(string? installationPath = default) : base(installationPath)
    {
    }

    public override string DownloadUrl => "https://microstock.plus/ESRGAN.zip";

    public override Func<CancellationToken, Task>? Installation => async cancellationToken =>
        await Task.Run(
            () => ZipFile.ExtractToDirectory(InstallerPath, InstallationPath ?? Path.Combine("plugins", "python")),
            cancellationToken);
}
