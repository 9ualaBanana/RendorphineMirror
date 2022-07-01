namespace Node.P2P.ResponseModels;

internal record UploadingStatus(
    string Name,
    string Ext,
    string UploadDirectory,
    long Size,
    long UploadedBytes,
    UploadedPacket[] UploadedChunks,
    long LastModified,
    string Type,
    string UserId,
    string Email,
    long Started,
    string IP)
{
}
