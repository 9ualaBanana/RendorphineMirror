using Benchmark;
using Node.P2P.ResponseModels;
using System.Diagnostics;
using System.Text.Json;

namespace Node.P2P;

internal class PacketsUploader : IDisposable
{
    readonly UploadSession _session;
    readonly RequestOptions _requestOptions;
    readonly UploadAdjuster _uploadAdjuster;
    int _offset;
    int _Offset
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

    internal PacketsUploader(
        UploadSession session,
        int initialPacketSize = 1024 * 128,
        int initialBatchSize = 1,
        int batchSizeLimit = 32)
    {
        _session = session;
        _requestOptions = session.RequestOptions;
        _fileStream = session.File.OpenRead();
        _packetSize = initialPacketSize;
        _batchSize = initialBatchSize;
        _uploadAdjuster = new(batchSizeLimit);
    }

    /// <remarks>
    /// Upload success should be checked with corresponding <see cref="UploadSession.EnsureAllBytesUploadedAsync"/>
    /// </remarks>
    internal async Task<BenchmarkResult> UploadAsync()
    {
        TimeSpan lastUploadTime;
        var uploadResult = new BenchmarkResult(_fileStream.Length);

        foreach (var range in _session.NotUploadedByteRanges)
        {
            _Offset = range.Start.Value;
            while (_Offset < range.End.Value)
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

    async IAsyncEnumerable<Packet> CreateBatchAsync(Range notUploadedBytesRange, int batchSize, int packetSize)
    {
        for (int packetsCount = 0; packetsCount < batchSize && _Offset < notUploadedBytesRange.End.Value; packetsCount++)
        {
            int bytesLeft = notUploadedBytesRange.End.Value - _Offset;
            if (bytesLeft < packetSize) packetSize = bytesLeft;

            var packet = new Packet(_session.FileId, _Offset, new byte[packetSize]);
            _Offset += await _fileStream.ReadAsync(
                packet.Content.AsMemory(0, packetSize)).ConfigureAwait(false);
            yield return packet;
        }
    }

    async Task UploadBatchAsync(IAsyncEnumerable<Packet> batch)
    {
        var uploaders = new List<Task<UploadingStatus>>();
        await foreach (var packet in batch)
        {
            uploaders.Add(UploadPacketAsync(packet));
        }
        await Task.WhenAll(uploaders);
    }

    async Task<UploadingStatus> UploadPacketAsync(Packet packet)
    {
        var packetHttpContent = new MultipartFormDataContent()
        {
            { new StringContent(packet.FileId), "fileid" },
            { new StringContent(packet.Offset.ToString()), "offset" },
            { new ByteArrayContent(packet.Content), "chunk", _session.FileName }
        };

        var uploadingStatusResponse = await Api.TrySendRequestAsync(
            async () => await _requestOptions.HttpClient.PostAsync(
                $"https://{_session.Hostname}/content/vcupload/chunk",
                packetHttpContent,
                _requestOptions.CancellationToken).ConfigureAwait(false),
            _session.RequestOptions).ConfigureAwait(false);
        return JsonDocument.Parse(await uploadingStatusResponse.Content.ReadAsStringAsync())
            .RootElement.GetProperty("fileinfo")
            .Deserialize<UploadingStatus>(new JsonSerializerOptions(JsonSerializerDefaults.Web))!;
    }

    public void Dispose()
    {
        _fileStream?.Dispose();
        GC.SuppressFinalize(this);
    }
}
