using System.IO.Compression;

namespace Node.Plugins.Deployment;

internal record BlenderDeploymentInfo : DownloadablePluginDeploymentInfo
{
    public BlenderDeploymentInfo(string? installationPath = default) : base(installationPath)
    {
    }

    public override string DownloadUrl => "https://mirrors.dotsrc.org/blender/release/Blender3.2/blender-3.2.2-windows-x64.zip";

    public override Func<CancellationToken, Task>? Installation => async cancellationToken =>
    {
        await Task.Run(() => ZipFile.ExtractToDirectory(InstallerPath, InstallationPath ?? _extractionDirectoryPath), cancellationToken);
        Directory.Move(_ExtractedDirectoryPath, _InstallationDirectoryPath);
    };

    #region Paths
    string _ExtractedDirectoryPath => Path.Combine(_extractionDirectoryPath, Path.GetFileNameWithoutExtension(DownloadUrl));
    readonly string _extractionDirectoryPath = Path.Combine($@"{Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)}", "Blender Foundation");
    string _InstallationDirectoryPath => Path.Combine(_extractionDirectoryPath, _installationDirectoryName);
    const string _installationDirectoryName = "Blender 3.2";
    #endregion
}
