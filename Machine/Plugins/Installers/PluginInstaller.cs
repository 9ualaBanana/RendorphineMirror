using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Machine.Plugins.Installers;

public abstract class PluginInstaller
{
    protected HttpClient HttpClient;
    protected CancellationToken CancellationToken;

    protected abstract string DownloadUrl { get; }

    public PluginInstaller(HttpClient httpClient, CancellationToken cancellationToken = default)
    {
        HttpClient = httpClient;
        CancellationToken = cancellationToken;
    }

    public async Task InstallAsync(bool deleteInstaller = true)
    {
        string installerFilePath = Path.Combine(DownloadsDirectoryPath, Path.GetFileName(DownloadUrl));

        if (!File.Exists(installerFilePath))
            await DownloadToAsync(installerFilePath);
        await InstallAsyncCore(installerFilePath);

        if (deleteInstaller) File.Delete(installerFilePath);
    }

    async Task DownloadToAsync(string path)
    {
        var pluginInstaller = await HttpClient.GetStreamAsync(DownloadUrl);
        using var fs = new FileStream(path, FileMode.CreateNew, FileAccess.Write);
        await pluginInstaller.CopyToAsync(fs);
    }

    async Task InstallAsyncCore(string installerPath)
    {
        using var installation = Process.Start(GetInstallationInfo(installerPath))!;
        await installation.WaitForExitAsync(CancellationToken);
    }

    protected abstract ProcessStartInfo GetInstallationInfo(string installerPath);

    static string DownloadsDirectoryPath => _downloadsDirectoryPath ??=
        SHGetKnownFolderPath(Guid.Parse("374DE290-123F-4565-9164-39C4925E467B"), default);
    static string? _downloadsDirectoryPath;

    [DllImport("shell32", CharSet = CharSet.Unicode, ExactSpelling = true, PreserveSig = false)]
    private static extern string SHGetKnownFolderPath(
        [MarshalAs(UnmanagedType.LPStruct)] Guid rfid, uint dwFlags, nint hToken = default);
}
