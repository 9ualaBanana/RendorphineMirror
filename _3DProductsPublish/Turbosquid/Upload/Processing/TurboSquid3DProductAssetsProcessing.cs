using _3DProductsPublish._3DProductDS;
using _3DProductsPublish.Turbosquid._3DModelComponents;
using _3DProductsPublish.Turbosquid.Api;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;

namespace _3DProductsPublish.Turbosquid.Upload.Processing;

internal partial class TurboSquid3DProductAssetsProcessing
{
    readonly HttpClient _httpClient;
    readonly TurboSquid3DProductUploadSessionContext _uploadSessionContext;

    internal TurboSquid3DProductAssetsProcessing(
        HttpClient httpClient,
        TurboSquid3DProductUploadSessionContext uploadSessionContext)
    {
        _httpClient = httpClient;
        _uploadSessionContext = uploadSessionContext;
    }

    internal async Task<Task_<_3DModel>> RunAsyncOn(_3DModel _3DModel, string assetUploadKey, CancellationToken cancellationToken)
    {
        using var archived3DModel = File.OpenRead(await _3DModel.ArchiveAsync(cancellationToken));
        var processingPayload = Payload.For(archived3DModel, assetUploadKey, _uploadSessionContext,
            string.Empty, string.Empty, string.Empty, false);

        return await CreateTaskAsync(processingPayload, _3DModel, cancellationToken);
    }

    internal async Task<Task_<TurboSquid3DProductThumbnail>> RunAsyncOn(TurboSquid3DProductThumbnail thumbnail, string assetUploadKey, CancellationToken cancellationToken)
        => await CreateTaskAsync(
            Payload.For(thumbnail, assetUploadKey, _uploadSessionContext),
            thumbnail, cancellationToken);

    async Task<Task_<TAsset>> CreateTaskAsync<TAsset>(Payload processingPayload, TAsset asset, CancellationToken cancellationToken)
        where TAsset : I3DProductAsset
    {
        var taskJson = await
            (await _httpClient.PostAsync(
                new Uri(TurboSquidApi._BaseUri, "turbosquid/uploads//process"),
                processingPayload.ToJson(),
                cancellationToken)
            )
            .EnsureSuccessStatusCode()
            .Content.ReadAsStringAsync(cancellationToken);

        return Task_<TAsset>.Create(JObject.Parse(taskJson), asset);
    }

    internal async Task<ITurboSquidProcessed3DProductAsset<TAsset>> AwaitAsyncOn<TAsset>(Task_<TAsset> processingTask, CancellationToken cancellationToken)
        where TAsset : I3DProductAsset
        => await AwaitAsyncOn(new List<Task_<TAsset>>(1) { processingTask }, cancellationToken).SingleAsync(cancellationToken);
    internal async IAsyncEnumerable<ITurboSquidProcessed3DProductAsset<TAsset>> AwaitAsyncOn<TAsset>(List<Task_<TAsset>> processingTasks, [EnumeratorCancellation] CancellationToken cancellationToken)
        where TAsset : I3DProductAsset
    {
        while (processingTasks.Any())
            foreach (var task in await UpdatedAsync(processingTasks, cancellationToken))
                if (task.IsCompleted)
                { processingTasks.Remove(task); yield return task.ToProcessedAsset(); }
    }
    internal async IAsyncEnumerable<ITurboSquidProcessed3DProductAsset<TAsset>> AwaitAsyncOn<TAsset>(IEnumerable<Task<Task_<TAsset>>> processingTasks, [EnumeratorCancellation] CancellationToken cancellationToken)
        where TAsset : I3DProductAsset
    {
        await foreach (var processedTask in AwaitAsyncOn((await Task.WhenAll(processingTasks)).ToList(), cancellationToken))
            yield return processedTask;
    }

    async Task<Task_<TAsset>> UpdatedAsync<TAsset>(Task_<TAsset> processingTask, CancellationToken cancellationToken)
        where TAsset : I3DProductAsset
        => (await UpdatedAsync(new List<Task_<TAsset>>(1) { processingTask }, cancellationToken)).Single();
    async Task<List<Task_<TAsset>>> UpdatedAsync<TAsset>(IEnumerable<Task_<TAsset>> processingTasks, CancellationToken cancellationToken)
        where TAsset : I3DProductAsset
    {
        var response = await
            (await _httpClient.PostAsJsonAsync(new Uri(TurboSquidApi._BaseUri, "turbosquid/uploads/bulk_poll"), new
            {
                authenticity_token = _uploadSessionContext.Credential._CsrfToken,
                ids = processingTasks.Select(_ => _.Id)
            }, cancellationToken))
            .EnsureSuccessStatusCode()
            .Content.ReadAsStringAsync(cancellationToken);

        // Zipped based on the apparent guarantee that JObjects returned as the response are in the same order as their IDs provided in the request.
        return JArray.Parse(response).Children().Zip(processingTasks)
            .Select(_ => new { UpdatedTask = _.First, _.Second.Asset })
            .Select(_ => Task_<TAsset>.Create(_.UpdatedTask, _.Asset))
            .ToList();
    }
}
