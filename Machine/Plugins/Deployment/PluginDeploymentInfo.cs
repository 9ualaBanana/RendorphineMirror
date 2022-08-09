using System.Runtime.InteropServices;

namespace Machine.Plugins.Deployment;

public abstract record PluginDeploymentInfo(string? InstallationPath = default)
{
    public string InstallerPath => Path.Combine(DownloadsDirectoryPath, Path.GetFileName(DownloadUrl));
    public abstract string DownloadUrl { get; }
    public bool RequiresInstallation => Installation is not null;
    public virtual Func<CancellationToken, Task>? Installation { get; }


    readonly static string DownloadsDirectoryPath = 
        SHGetKnownFolderPath(Guid.Parse("374DE290-123F-4565-9164-39C4925E467B"), default);

    [DllImport("shell32", CharSet = CharSet.Unicode, ExactSpelling = true, PreserveSig = false)]
    static extern string SHGetKnownFolderPath(
        [MarshalAs(UnmanagedType.LPStruct)] Guid rfid, uint dwFlags, nint hToken = default);
}
