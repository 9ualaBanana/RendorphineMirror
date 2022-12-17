using Microsoft.Net.Http.Headers;
using Transport.Models;
using Transport.Upload._3DModelsUpload.CGTrader._3DModelComponents;
using Transport.Upload._3DModelsUpload.CGTrader.Upload;
using Transport.Upload._3DModelsUpload.Turbosquid.Upload;

namespace Transport.Upload._3DModelsUpload.Turbosquid.Api;

internal class TurboSquidUploadApi : IBaseAddressProvider
{
    readonly HttpClient _httpClient;
    readonly TurboSquidAwsUploadCredentials _awsCredentials;

    public string BaseAddress { get; init; }

    internal TurboSquidUploadApi(HttpClient httpClient, TurboSquidAwsUploadCredentials awsCredentials)
    {
        _httpClient = httpClient;
        _awsCredentials = awsCredentials;
        BaseAddress = $"https://{awsCredentials.Bucket}.s3.amazonaws.com/{awsCredentials.KeyPrefix}{DateTime.UtcNow.AsUnixTimestamp()}/";
    }

    internal async Task _UploadAssetsAsync(Composite3DModelDraft modelDraft, CancellationToken cancellationToken)
    {
        foreach (var _3DModel in modelDraft._Model._3DModels)
            foreach (var modelFilePath in _3DModel.Files)
                await _UploadAssetAsync(modelFilePath, modelDraft._DraftID, cancellationToken);

        //foreach (var modelPreviewImage in modelDraft._UpcastPreviewImagesTo<CGTrader3DModelPreviewImage>())
        //{
        //    string uploadedFileId = await _UploadAssetAsync(modelPreviewImage, modelDraft, cancellationToken);
        //    (modelDraft._Model.Metadata as TurboSquid3DModelPreviewImage)!.UploadedPreviewImagesIDs.Add(uploadedFileId);
        //}
    }

    async Task _UploadAssetAsync(string assetPath, string productDraftId, CancellationToken cancellationToken)
    {
        await _SendAssetUploadOptionsAsyncFor(assetPath, cancellationToken);
        await _AuthorizeAssetUploadAsyncFor(assetPath, cancellationToken);
    }

    async Task _SendAssetUploadOptionsAsyncFor(string assetPath, CancellationToken cancellationToken)
    {
        var request = new HttpRequestMessage(HttpMethod.Options, (this as IBaseAddressProvider).Endpoint($"{Path.GetFileName(assetPath)}?uploads"));
        request.Headers.Add("Access-Control-Request-Headers", string.Join(',',
            HeaderNames.Authorization.ToLower(),
            HeaderNames.ContentType.ToLower(),
            "x-amz-acl",
            "x-amz-content-sha256",
            "x-amz-date",
            "x-amz-security-token"));
        request.Headers.Add("Access-Control-Request-Method", HttpMethod.Post.Method);
        request.Headers.Add("Origin", "https://www.squid.io");

        (await _httpClient.SendAsync(request, cancellationToken)).EnsureSuccessStatusCode();
    }

    async Task _AuthorizeAssetUploadAsyncFor(string assetPath, CancellationToken cancellationToken)
    {
        var request = await new HttpRequestMessage(HttpMethod.Post, (this as IBaseAddressProvider).Endpoint($"{Path.GetFileName(assetPath)}?uploads"))
        { Content = new ByteArrayContent(Array.Empty<byte>()) }// Content-Type is not set to application/octet-stream; charset=UTF-8 automatically but the server doesn't mind so far.
        ._SignedAsyncWith(_awsCredentials);

        (await _httpClient.SendAsync(request, cancellationToken)).EnsureSuccessStatusCode();
    }
}
