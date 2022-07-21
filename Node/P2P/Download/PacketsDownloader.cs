using Node.P2P.Models;
using System.Net;

namespace Node.P2P.Download;

// Launch the listener for download initializations on the downloader side.
// Send GET request to the uploader to notify him that the downloader is started. (it's likely unnecessary)
// Uploader sends the init POST request with the data about the file that it'll send.
// Downloader creates the .part file and the corresponding PacketsDownloader for that file.
// That PacketsDownloader launches the listener with the GUID prefix to the endpoints which is sent to the uploader.
// Uploader uses that endpoint as the Host to which upload the files.
internal class PacketsDownloader : IEquatable<PacketsDownloader>
{
    // Differentiates between multiple downloads. Has the function similar to ports.
    internal string Id => _filesManager.FileId;
    internal long DownloadedBytesCount => _filesManager.DownloadedBytes.Aggregate(0L, (total, range) => total + range.Length);
    IEnumerable<UploadedPacket>? _downloadedPackets;
    internal IEnumerable<UploadedPacket> DownloadedPackets => _downloadedPackets ??=
        _filesManager.DownloadedBytes.Select(range => new UploadedPacket(range.Start, (int)range.Length, 1));
    readonly DownloadFilesManager _filesManager;
    readonly HttpListener _uploadRequestsListener = new();

    internal event EventHandler? PacketReceived;
    internal event EventHandler? DownloadStopped;
    internal event EventHandler<string>? DownloadCompleted;

    internal PacketsDownloader(DownloadFileInfo fileToDownload)
    {
        _filesManager = new(fileToDownload);
        _uploadRequestsListener.Prefixes.Add($"http://*:{PortForwarding.Port}/{Id}/content/vcupload/");
    }

    internal async Task StartAsync(CancellationToken cancellationToken)
    {
        try { _uploadRequestsListener.Start(); }
        catch (HttpListenerException ex) { Console.WriteLine($"{typeof(PacketsDownloader).Name} couldn't start:\n{ex}"); }

        while (_uploadRequestsListener.IsListening)
        {
            var context = await _uploadRequestsListener.GetContextAsync();
            await HandleUploadRequestAsync(context, cancellationToken);
        }
    }

    async Task HandleUploadRequestAsync(HttpListenerContext context, CancellationToken cancellationToken)
    {
        var lastSubpathSegment = context.Request.RawUrl?
            .Split("/", StringSplitOptions.RemoveEmptyEntries)
            .Last();
        try
        {
            switch (lastSubpathSegment)
            {
                case "chunk":
                    await DownloadPacketAsync(context, cancellationToken);
                    break;

                case "finish":
                    FinalizeDownload(context.Response);
                    break;
            }
        }
        catch (Exception ex) { Respond.NotOk(context.Response, ex.Message); }
    }

    async Task DownloadPacketAsync(HttpListenerContext context, CancellationToken cancellationToken)
    {
        PacketReceived?.Invoke(this, EventArgs.Empty);
        var packet = await Packet.DeserializeAsync(context.Request);
        await _filesManager.DownloadPacketAsync(packet, cancellationToken);
        Respond.Ok(context.Response);
        await packet.Content.DisposeAsync();
    }

    void FinalizeDownload(HttpListenerResponse response)
    {
        _filesManager.FinalizeDownload();
        Respond.Ok(response);
        _uploadRequestsListener.Close();
        DownloadCompleted?.Invoke(this, _filesManager.FullName);
    }

    internal void StopDownload()
    {
        _uploadRequestsListener.Close();
        DownloadStopped?.Invoke(this, EventArgs.Empty);
    }


    #region EqualityContract
    public bool Equals(PacketsDownloader? other)
    {
        return Id == other?.Id;
    }

    public override bool Equals(object? obj)
    {
        return Equals(obj as PacketsDownloader);
    }

    public override int GetHashCode()
    {
        return Id.GetHashCode();
    }
    #endregion
}
