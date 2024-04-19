namespace _3DProductsPublish.CGTrader.Upload;

internal abstract record CGTrader3DModelAssetUploadSessionData
{
    internal readonly string _FilePath;
    internal readonly string _StorageLocation;
    internal readonly long _FileID;

    protected CGTrader3DModelAssetUploadSessionData(
        string filePath,
        string storageLocation,
        long fileId)
    {
        _FilePath = filePath;
        _FileID = fileId;
        _StorageLocation = storageLocation;
    }

    internal static async Task<CGTrader3DModelFileUploadSessionData> _ForModelFileAsyncFrom(
        HttpResponseMessage response,
        string modelFilePath,
        CancellationToken cancellationToken) =>
            await CGTrader3DModelFileUploadSessionData._AsyncFrom(response, modelFilePath, cancellationToken);

    internal static async Task<CGTrader3DModelPreviewImageUploadSessionData> _ForModelThumbnailAsyncFrom(
        HttpResponseMessage response,
        string modelPreviewImageFilePath,
        CancellationToken cancellationToken) =>
            await CGTrader3DModelPreviewImageUploadSessionData._AsyncFrom(response, modelPreviewImageFilePath, cancellationToken);

    internal abstract Task UseToUploadWith(HttpClient httpClient, HttpMethod httpMethod, CancellationToken cancellationToken);
}
