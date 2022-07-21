namespace Node.P2P.Models;

internal record struct UploadedPacket(
    long Offset,
    int Length,
    int Written)
{
}
