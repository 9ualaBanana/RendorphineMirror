using Transport.Models;

namespace Transport.Upload;

internal record RequestedUploadSessionData(
    string FileId,
    string Host,
    long UploadedBytes,
    UploadedPacket[] UploadedChunks)
{
}
