namespace Node.P2P;

internal record Packet(
    string FileName,
    string FileId,
    long Offset,
    byte[] Content)
{
    internal MultipartFormDataContent AsHttpContent => new()
    {
        { new StringContent(FileId), "fileid" },
        { new StringContent(Offset.ToString()), "offset" },
        { new ByteArrayContent(Content), "chunk", FileName }
    };
}
