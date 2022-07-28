using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Machine.Plugins.Installers;

internal abstract record PluginDeploymentInfo(string? InstallationPath = default)
{
    internal string InstallerPath => Path.Combine(DownloadsDirectoryPath, Path.GetFileName(DownloadUrl));
    internal abstract string DownloadUrl { get; }
    internal abstract ProcessStartInfo InstallationStartInfo { get; }


    readonly static string DownloadsDirectoryPath = 
        SHGetKnownFolderPath(Guid.Parse("374DE290-123F-4565-9164-39C4925E467B"), default);

    [DllImport("shell32", CharSet = CharSet.Unicode, ExactSpelling = true, PreserveSig = false)]
    static extern string SHGetKnownFolderPath(
        [MarshalAs(UnmanagedType.LPStruct)] Guid rfid, uint dwFlags, nint hToken = default);
}
