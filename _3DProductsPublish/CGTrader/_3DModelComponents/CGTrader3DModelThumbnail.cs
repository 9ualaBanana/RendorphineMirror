using _3DProductsPublish._3DModelDS;
using System.Security.Cryptography;

namespace _3DProductsPublish.CGTrader._3DModelComponents;

internal record CGTrader3DModelThumbnail : _3DProductThumbnail
{
    internal async ValueTask<string> ChecksumAsync(CancellationToken cancellationToken)
    {
        if (_checksum is null)
        {
            using var hasher = MD5.Create();
            using var fileStream = AsFileStream;
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
