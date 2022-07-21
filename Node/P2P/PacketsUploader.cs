using Benchmark;
using Node.P2P.Models;
using System.Diagnostics;

namespace Node.P2P;

internal class PacketsUploader : IDisposable
{
    readonly UploadSession _session;
    readonly UploadAdjuster _uploadAdjuster;
    long _offset;
    long _Offset
    {
        get => _offset;
        set
        {
            _offset = value;
            _fileStream.Position = value;
        }
    }
    readonly FileStream _fileStream;

    int _packetSize;
    int _batchSize;

    readonly HttpClient _httpClient;
    readonly CancellationToken _cancellationToken;

    internal PacketsUploader(
        UploadSession session,
        HttpClient httpClient,
        CancellationToken cancellationToken,
        int initialPacketSize = 1024 * 128,
        int initialBatchSize = 1,
        int batchSizeLimit = 32)
    {
        _session = session;
        _fileStream = session.Data.File.OpenRead();
        _packetSize = initialPacketSize;
        _batchSize = initialBatchSize;
        _uploadAdjuster = new(batchSizeLimit);
        _httpClient = httpClient;
        _cancellationToken = cancellationToken;
    }

    /// <remarks>
    /// Upload success status should be checked with corresponding <see cref="UploadSession.EnsureAllBytesUploadedAsync"/>
    /// </remarks>
    internal async Task<BenchmarkResult> UploadAsync()
    {
        TimeSpan lastUploadTime;
        var uploadResult = new BenchmarkResult(_fileStream.Length);

        foreach (var range in _session.NotUploadedBytes)
        {
            _Offset = range.Start;
            while (_Offset < range.End)
            {
                var batch = CreateBatchAsync(range, _batchSize, _packetSize);

                var sw = Stopwatch.StartNew();

                try { await UploadBatchAsync(batch).ConfigureAwait(false); }
                catch (HttpRequestException)
                {
                    sw.Stop();
                    uploadResult.Time += sw.Elapsed;
                    return uploadResult;
                }

                sw.Stop();
                uploadResult.Time += lastUploadTime = sw.Elapsed;

                _uploadAdjuster.Adjust(ref _packetSize, ref _batchSize, lastUploadTime);
            }
        }
        return uploadResult;
    }

    async IAsyncEnumerable<Packet> CreateBatchAsync(LongRange notUploadedBytesRange, int batchSize, int packetSize)
    {
        for (int packetsCount = 0; packetsCount < batchSize && _Offset < notUploadedBytesRange.End; packetsCount++)
        {
            long bytesLeft = notUploadedBytesRange.End - _Offset;
            if (bytesLeft < packetSize) packetSize = (int)bytesLeft;

            yield return await CreatePacketAsync(packetSize);
        }
    }

    async Task<Packet> CreatePacketAsync(int size)
    {
        var packet = new Packet(_session.Data.File.Name, _session.FileId, _Offset, new MemoryStream(size));

        var contentBuffer = new Memory<byte>(new byte[size]);
        _Offset += await _fileStream.ReadAsync(contentBuffer, _cancellationToken).ConfigureAwait(false);
        await packet.Content.WriteAsync(contentBuffer, _cancellationToken);
        packet.Content.Position = 0;

        return packet;
    }

    async Task UploadBatchAsync(IAsyncEnumerable<Packet> batch)
    {
        var uploaders = new List<Task>();
        await foreach (var packet in batch)
        {
            uploaders.Add(UploadPacketAsync(packet));
        }
        await Task.WhenAll(uploaders);
    }

    async Task UploadPacketAsync(Packet packet)
    {
        await _httpClient.PostAsync(
            $"https://{_session.Host}/content/vcupload/chunk",
            await packet.ToHttpContentAsync()).ConfigureAwait(false);
        await packet.Content.DisposeAsync();
    }

    public void Dispose()
    {
        _fileStream?.Dispose();
        GC.SuppressFinalize(this);
    }
}
