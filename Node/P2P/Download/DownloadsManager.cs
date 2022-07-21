using Node.P2P.Models;
using System.Net;
using System.Text.Json;
using Timer = System.Timers.Timer;

namespace Node.P2P.Download;

internal static class DownloadsManager
{
    static readonly HttpListener _httpListener = new();
    static CancellationToken _cancellationToken = default;
    static readonly Dictionary<PacketsDownloader, Timer> ActiveDownloads = new();

    internal static event EventHandler<PacketsDownloader>? DownloadStarted;
    internal static event EventHandler<string>? DownloadCompleted;

    static DownloadsManager() { _httpListener.Prefixes.Add($"http://*:{PortForwarding.Port}/initupload/"); }

    internal static async Task StartAcceptingDownloadsAsync(CancellationToken cancellationToken = default)
    {
        if (_httpListener.IsListening) return;

        try { _httpListener.Start(); }
        catch (HttpListenerException ex) { Console.WriteLine($"{typeof(DownloadsManager).Name} couldn't start:\n{ex}"); }
        _cancellationToken = cancellationToken;

        while (_httpListener.IsListening)
        {
            var context = await _httpListener.GetContextAsync();
            var downloader = await InitializeDownloadAsync(context.Request);
            Respond.Ok(context.Response, await BuildResponseBodyAsync(downloader));
        }
    }

    static async Task<PacketsDownloader> InitializeDownloadAsync(HttpListenerRequest request)
    {
        var downloader = new PacketsDownloader(await DownloadFileInfo.DeserializeAsync(request));
        // That's bullshit, implement a better way of resolving name conflicts.
        if (!ActiveDownloads.ContainsKey(downloader))
            StartDownloader(downloader);

        return downloader;
    }

    static async Task<IEnumerable<KeyValuePair<string, object?>>> BuildResponseBodyAsync(PacketsDownloader downloader)
    {
        return new Dictionary<string, object?>
        {
            ["fileid"] = downloader.Id,
            ["host"] = $"d7e4-213-87-159-225.eu.ngrok.io/{downloader.Id}",
            //["host"] = $"{await MachineInfo.GetPublicIPAsync()}:{PortForwarding.Port}/{downloader.Id}",
            ["uploadedbytes"] = downloader.DownloadedBytesCount,
            ["uploadedchunks"] = downloader.DownloadedPackets.OrderBy(packet => packet.Offset)
        };
    }

    static void StartDownloader(PacketsDownloader downloader)
    {
        downloader.PacketReceived += OnPacketDownloaded;
        downloader.DownloadStopped += OnDownloadStopped;
        downloader.DownloadCompleted += OnDownloadCompleted;

        var downloadTimeoutTimer = new Timer(TimeSpan.FromMinutes(5).TotalMilliseconds);
        downloadTimeoutTimer.Elapsed += OnDownloadTimeout;
        downloadTimeoutTimer.Start();

        ActiveDownloads.Add(downloader, downloadTimeoutTimer);
        _ = downloader.StartAsync(_cancellationToken);
        DownloadStarted?.Invoke(null, downloader);
    }

    static void OnDownloadCompleted(object? downloader, string downloadedFileName)
    {
        OnDownloadStopped(downloader, EventArgs.Empty);
        DownloadCompleted?.Invoke(null, downloadedFileName);
    }

    static void OnDownloadStopped(object? downloader, EventArgs e) =>
        RemoveDownloader((PacketsDownloader)downloader!);

    static void RemoveDownloader(PacketsDownloader downloader) => ActiveDownloads.Remove(downloader);

    static void OnPacketDownloaded(object? downloader, EventArgs e)
    {
        var downloadTimeoutTimer = ActiveDownloads[(PacketsDownloader)downloader!];
        downloadTimeoutTimer.Stop();
        downloadTimeoutTimer.Start();
    }

    static void OnDownloadTimeout(object? sender, System.Timers.ElapsedEventArgs e)
    {
        try
        {
            var downloader = ActiveDownloads.Single(d => d.Value == sender).Key;
            downloader.StopDownload();
        }
        catch (Exception) { }
    }

    internal static void StopAcceptingDownloads() { _httpListener.Stop(); }
}
