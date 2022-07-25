using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Machine.Plugins.Installers;

public abstract class PluginInstaller
{
    readonly HttpClient _httpClient;
    readonly CancellationToken _cancellationToken;

    protected abstract string DownloadUrl { get; }

    public PluginInstaller(HttpClient httpClient, CancellationToken cancellationToken = default)
    {
        _httpClient = httpClient;
        _cancellationToken = cancellationToken;
    }

    public async Task InstallAsync(string? installationPath = default, bool deleteInstaller = true)
    {
        string installerPath = Path.Combine(DownloadsDirectoryPath.Value, Path.GetFileName(DownloadUrl));

        if (!File.Exists(installerPath))
            await DownloadToAsync(installerPath);
        await InstallAsync(installerPath, installationPath);

        if (deleteInstaller) File.Delete(installerPath);
    }

    async Task DownloadToAsync(string path)
    {
        var pluginInstaller = await _httpClient.GetStreamAsync(DownloadUrl, _cancellationToken);
        using var fs = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None, 4096, true);
        await pluginInstaller.CopyToAsync(fs, _cancellationToken);
    }

    async Task InstallAsync(string installerPath, string? installationPath = default)
    {
        using var installation = Process.Start(GetInstallationInfo(installerPath, installationPath))!;
        await installation.WaitForExitAsync(_cancellationToken);
    }

    protected abstract ProcessStartInfo GetInstallationInfo(string installerPath, string? installationPath = default);

    readonly static Lazy<string> DownloadsDirectoryPath = new(
        () => SHGetKnownFolderPath(Guid.Parse("374DE290-123F-4565-9164-39C4925E467B"), default),
        isThreadSafe: false);

    [DllImport("shell32", CharSet = CharSet.Unicode, ExactSpelling = true, PreserveSig = false)]
    private static extern string SHGetKnownFolderPath(
        [MarshalAs(UnmanagedType.LPStruct)] Guid rfid, uint dwFlags, nint hToken = default);
}
