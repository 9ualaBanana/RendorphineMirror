using _3DProductsPublish._3DProductDS;
using System.Security.Cryptography;

namespace _3DProductsPublish.CGTrader._3DModelComponents;

internal class CGTrader3DModelThumbnail : _3DProductThumbnail
{
    internal async ValueTask<string> ChecksumAsync(CancellationToken cancellationToken)
    {
        if (_checksum is null)
        {
            using var hasher = MD5.Create();
            using var fileStream = File.OpenRead(FilePath);
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
