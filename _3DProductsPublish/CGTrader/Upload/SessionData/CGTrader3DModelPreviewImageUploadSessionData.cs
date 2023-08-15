using _3DProductsPublish.CGTrader.Network;
using System.Net.Http.Headers;
using System.Web;

namespace _3DProductsPublish.CGTrader.Upload.SessionData;

internal record CGTrader3DModelPreviewImageUploadSessionData : CGTrader3DModelAssetUploadSessionData
{
    internal readonly string _SignedFileID;
    internal readonly MediaTypeHeaderValue _ContentType;
    internal readonly string _ContentMD5;
    internal readonly string _XAmzAlgorithm;
    internal readonly string _XAmzCredential;
    internal readonly string _XAmzDate;
    internal readonly string _XAmzExpires;
    internal readonly string _XAmzSignedHeaders;
    internal readonly string _XAmzSignature;

    internal static async Task<CGTrader3DModelPreviewImageUploadSessionData> _AsyncFrom(
        HttpResponseMessage response,
        string modelPreviewImageFilePath,
        CancellationToken cancellationToken)
    {
        var previewImageUploadSessionDataJson = JObject.Parse
            (await response.Content.ReadAsStringAsync(cancellationToken)
            )["data"]!["attributes"]!;
        string storageLocation = (string)previewImageUploadSessionDataJson["url"]!;
        var parsedQueryString = HttpUtility.ParseQueryString(storageLocation);

        return new CGTrader3DModelPreviewImageUploadSessionData(
            modelPreviewImageFilePath,
            storageLocation,
            (string)previewImageUploadSessionDataJson["blobId"]!,
            (string)previewImageUploadSessionDataJson["signedBlobId"]!,
            (string)previewImageUploadSessionDataJson["headers"]!["Content-Type"]!,
            (string)previewImageUploadSessionDataJson["headers"]!["Content-MD5"]!,
            parsedQueryString["X-Amz-Algorithm"]!,
            parsedQueryString["X-Amz-Credential"]!,
            parsedQueryString["X-Amz-Date"]!,
            parsedQueryString["X-Amz-Expires"]!,
            parsedQueryString["X-Amz-SignedHeaders"]!,
            parsedQueryString["X-Amz-Signature"]!);
    }

    CGTrader3DModelPreviewImageUploadSessionData(
        string modelPreviewImageFilePath,
        string storageLocation,
        string fileId,
        string signedFileId,
        string contentType,
        string contentMd5,
        string xAmzAlgorithm,
        string xAmzCredential,
        string xAmzDate,
        string xAmzExpires,
        string xAmzSignedHeaders,
        string xAmzSignature) : base(modelPreviewImageFilePath, storageLocation, fileId)
    {
        _SignedFileID = signedFileId;
        _ContentType = new MediaTypeHeaderValue(contentType);
        _ContentMD5 = contentMd5;
        _XAmzAlgorithm = xAmzAlgorithm;
        _XAmzCredential = xAmzCredential;
        _XAmzDate = xAmzDate;
        _XAmzExpires = xAmzExpires;
        _XAmzSignature = xAmzSignature;
        _XAmzSignedHeaders = xAmzSignedHeaders;
        _XAmzSignature = xAmzSignature;
    }

    internal override async Task _UseToUploadWith(HttpClient httpClient, HttpMethod httpMethod, CancellationToken cancellationToken)
    {
        if (httpMethod == HttpMethod.Options) (await httpClient.SendAsync(
            new HttpRequestMessage(httpMethod, _StorageLocation)._ConfiguredAsModelPreviewImageUploadOptions(), cancellationToken))
            .EnsureSuccessStatusCode();
        else if (httpMethod == HttpMethod.Put)
        {
            using var modelPreviewImageFileStream = File.OpenRead(_FilePath);
            (await httpClient.SendAsync(
                new HttpRequestMessage(httpMethod, _StorageLocation)
                { Content = new StreamContent(modelPreviewImageFileStream) }._WithContentHeaders(_ContentType, _ContentMD5),
                cancellationToken))
            .EnsureSuccessStatusCode();
        }
    }
}

static class Extensions
{
    internal static HttpRequestMessage _ConfiguredAsModelPreviewImageUploadOptions(this HttpRequestMessage request)
    {
        request.Headers.Add("Origin", CGTraderUri.Https);
        request.Headers.Host = "images-cgtrader-com.s3.amazonaws.com";
        request.Headers.Add("Access-Control-Request-Headers", "content-md5,content-type");
        request.Headers.Add("Access-Control-Request-Method", "PUT");
        return request;
    }

    internal static HttpRequestMessage _WithContentHeaders(
        this HttpRequestMessage request,
        MediaTypeHeaderValue contentType,
        string contentMd5)
    {
        request.Content!.Headers.ContentType = contentType;
        request.Content!.Headers.Add(Microsoft.Net.Http.Headers.HeaderNames.ContentMD5, contentMd5);
        return request;
    }
}
