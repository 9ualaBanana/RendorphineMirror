using _3DProductsPublish._3DModelDS;
using _3DProductsPublish.Turbosquid._3DModelComponents;
using _3DProductsPublish.Turbosquid.Api;
using System.Diagnostics.CodeAnalysis;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;

namespace _3DProductsPublish.Turbosquid.Upload;

internal class TurboSquid3DProductAssetsProcessor
{
    readonly HttpClient _httpClient;
    readonly TurboSquid3DProductUploadSessionContext _uploadSessionContext;

    internal TurboSquid3DProductAssetsProcessor(
        HttpClient httpClient,
        TurboSquid3DProductUploadSessionContext uploadSessionContext)
    {
        _httpClient = httpClient;
        _uploadSessionContext = uploadSessionContext;
    }

    internal async Task<Task_<_3DModel>> RunAsyncOn(_3DModel _3DModel, string assetUploadKey, CancellationToken cancellationToken)
    {
        using var archived3DModel = File.OpenRead(await _3DModel.ArchiveAsync(cancellationToken));
        var processingPayload = TurboSquid3DProductAssetProcessingPayload.For(archived3DModel, assetUploadKey, _uploadSessionContext,
            string.Empty, string.Empty, string.Empty, false);

        return await CreateTaskAsync(processingPayload, _3DModel, cancellationToken);
    }

    internal async Task<Task_<TurboSquid3DProductThumbnail>> RunAsyncOn(TurboSquid3DProductThumbnail thumbnail, string assetUploadKey, CancellationToken cancellationToken)
    {
        var processingPayload = TurboSquid3DProductAssetProcessingPayload.For(thumbnail, assetUploadKey, _uploadSessionContext);

        return await CreateTaskAsync(processingPayload, thumbnail, cancellationToken);
    }

    async Task<Task_<TAsset>> CreateTaskAsync<TAsset>(TurboSquid3DProductAssetProcessingPayload processingPayload, TAsset asset, CancellationToken cancellationToken)
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


    internal class Task_<TAsset> : IEquatable<Task_<TAsset>>
        where TAsset : I3DProductAsset
    {
        internal static Task_<TAsset> Create(JToken taskJson, TAsset asset)
        {
            var task = taskJson.ToObject<Task_<TAsset>>()!; task.Asset = asset;
            return task;
        }

        internal TAsset Asset { get; private set; } = default!;

        [JsonProperty("id")]
        internal string Id { get; init; } = default!;

        internal bool IsCompleted => Status == "success";

        [JsonProperty("status")]
        string Status { get; init; } = default!;

        [JsonProperty("file_id")]
        [MemberNotNullWhen(true, nameof(IsCompleted))]
        internal string? FileId { get; init; } = default!;

        internal ITurboSquidProcessed3DProductAsset<TAsset> ToProcessedAsset()
            => IsCompleted ? TurboSquidProcessed3DProductAssetFactory.Create(Asset, FileId!)
            : throw new InvalidOperationException($"{Asset.GetType()} asset is not processed yet.");

        #region EqualityContract

        public override bool Equals(object? obj) => Equals(obj as Task_<TAsset>);
        public bool Equals(Task_<TAsset>? other) => Id.Equals(other?.Id);
        public override int GetHashCode() => Id.GetHashCode();

        #endregion
    }
}
