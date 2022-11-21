using Common;
using System.Net.Mime;
using System.Security.Cryptography;

namespace Transport.Upload._3DModelsUpload.CGTrader.Models;

internal record ModelPreview
{
    internal readonly FileStream FileStream;
    internal string FileName => Path.GetFileName(FileStream.Name);
    internal readonly string ModelDraftID;
    internal ContentType MimeType => new(MimeTypes.GetMimeType(FileStream.Name));
    internal async ValueTask<string> ChecksumAsync(CancellationToken cancellationToken)
    {
        if (_checksum is null)
        {
            using var hasher = new HMACMD5();
            _checksum = Convert.ToBase64String(
                await hasher.ComputeHashAsync(FileStream, cancellationToken)
                );
        }
        return _checksum;
    }
    string? _checksum;

    internal string? FileID
    {
        get => _fileId;
        set
        {
            if (_fileId is not null) throw new InvalidOperationException(
                $"{nameof(FileID)} is lateinit"
                );
            else _fileId = value;
        }
    }
    string? _fileId;

    internal string? SignedFileID
    {
        get => _signedFileId;
        set
        {
            if (_signedFileId is not null) throw new InvalidOperationException(
                $"{nameof(SignedFileID)} is lateinit"
                );
            else _signedFileId = value;
        }
    }
    string? _signedFileId;

    internal string? LocationOnServer
    {
        get => _locationOnServer;
        set
        {
            if (_locationOnServer is not null) throw new InvalidOperationException(
                $"{nameof(LocationOnServer)} is lateinit"
                );
            else _locationOnServer = value;
        }
    }
    string? _locationOnServer;

    internal ModelPreview(
        FileStream fileStream,
        string modelDraftId)
    {
        FileStream = fileStream;
        ModelDraftID = modelDraftId;
    }
}
