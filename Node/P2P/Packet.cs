namespace Node.P2P;

internal record Packet(
    string FileId,
    long Offset,
    byte[] Content)
{
}
