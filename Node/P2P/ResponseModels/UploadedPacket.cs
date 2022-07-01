namespace Node.P2P.ResponseModels;

internal record struct UploadedPacket(
    long Offset,
    int Length,
    int Written)
{
}
