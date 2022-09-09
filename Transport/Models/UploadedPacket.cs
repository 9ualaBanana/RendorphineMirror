namespace Transport.Models;

internal record struct UploadedPacket(
    long Offset,
    int Length,
    int Written)
{
}
