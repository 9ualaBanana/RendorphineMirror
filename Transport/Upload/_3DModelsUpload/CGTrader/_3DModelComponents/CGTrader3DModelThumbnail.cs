using System.Security.Cryptography;
using Transport.Upload._3DModelsUpload._3DModelDS;

namespace Transport.Upload._3DModelsUpload.CGTrader._3DModelComponents;

internal record CGTrader3DModelThumbnail : _3DModelThumbnail
{
    internal async ValueTask<string> ChecksumAsync(CancellationToken cancellationToken)
    {
        if (_checksum is null)
        {
            using var hasher = MD5.Create();
            using var fileStream = this.AsFileStream;
            _checksum = Convert.ToBase64String(
                await hasher.ComputeHashAsync(fileStream, cancellationToken)
                );
        }
        return _checksum;
    }
    string? _checksum;

    internal CGTrader3DModelThumbnail(string path) : base(path)
    {
    }
}
