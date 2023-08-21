using System.Diagnostics.CodeAnalysis;

namespace _3DProductsPublish.Turbosquid.Upload.Processing;

internal partial class TurboSquid3DProductAssetsProcessing
{
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