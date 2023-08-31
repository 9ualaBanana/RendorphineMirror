using _3DProductsPublish.Turbosquid.Api;
using System.Diagnostics.CodeAnalysis;
using System.Net.Http.Json;
using static _3DProductsPublish.Turbosquid.Upload.Processing.TurboSquid3DProductAssetProcessing;

namespace _3DProductsPublish.Turbosquid.Upload.Processing;

internal partial class TurboSquid3DProductAssetProcessing
{
    internal class Task_<TAsset> : Task<ITurboSquidProcessed3DProductAsset<TAsset>>, IEquatable<Task_<TAsset>>
        where TAsset : I3DProductAsset
    {
        new internal string Id { get; }

        readonly ServerTask _serverTask;
        readonly TAsset _asset;
        readonly Context _context;

        internal static async Task<Task_<TAsset>> RunAsync(TAsset asset, string uploadKey, TurboSquid3DProductUploadSessionContext context, HttpClient assetsProcessor, CancellationToken cancellationToken)
            => await RunAsync(asset, Payload.For(asset, uploadKey, context), assetsProcessor, TimeSpan.FromMilliseconds(1000), cancellationToken);
        internal static async Task<Task_<TAsset>> RunAsync(TAsset asset, string uploadKey, TurboSquid3DProductUploadSessionContext context, TimeSpan pollInterval, HttpClient assetsProcessor, CancellationToken cancellationToken)
            => await RunAsync(asset, Payload.For(asset, uploadKey, context), assetsProcessor, pollInterval, cancellationToken);
        static async Task<Task_<TAsset>> RunAsync(TAsset asset, Payload payload, HttpClient assetsProcessor, TimeSpan pollInterval, CancellationToken cancellationToken)
            => await RunAsync(asset, new Context(payload, assetsProcessor, (int)pollInterval.TotalMilliseconds, cancellationToken));
        static async Task<Task_<TAsset>> RunAsync(TAsset asset, Context context)
        {
            return new(await QueueTaskAsync(), asset, context);


            async Task<ServerTask> QueueTaskAsync()
                => JObject.Parse(await
                    (await context.AssetsProcessor.PostAsync(
                        new Uri(TurboSquidApi._BaseUri, "turbosquid/uploads//process"),
                        context.Payload.ToJson(),
                        context.CancellationToken)
                    )
                    .EnsureSuccessStatusCode()
                    .Content.ReadAsStringAsync(context.CancellationToken))
                .ToObject<ServerTask>()!;
        }


        internal async Task<Task_<TAsset>> RestartedAsync()
            => await RestartedAsync(_context.CancellationToken);
        internal async Task<Task_<TAsset>> RestartedAsync(CancellationToken cancellationToken)
            => await RunAsync(_asset, _context with { CancellationToken = cancellationToken });

        static Task_<TAsset> Updated(Task_<TAsset> task, ServerTask updatedServerTask)
            => new(updatedServerTask, task._asset, task._context);
        Task_(ServerTask serverTask, TAsset asset, Context context)
            : base(() => Core(serverTask, asset, context).Result, context.CancellationToken, TaskCreationOptions.LongRunning)
        {
            Id = serverTask.Id;
            _serverTask = serverTask;
            _asset = asset;
            _context = context;
        }

        static async Task<ITurboSquidProcessed3DProductAsset<TAsset>> Core(ServerTask serverTask, TAsset asset, Context context)
        {
            while (true)
            {
                serverTask = await UpdateAsync(serverTask, context);
                if (serverTask.IsCompletedSuccessfully)
                    return TurboSquidProcessed3DProductAssetFactory.Create(asset, serverTask.FileId!);
                else if (serverTask.IsFailed)
                    throw new HttpRequestException("Asset processing task has failed due to an unknown exception.");

                await Delay(context.PollInterval, context.CancellationToken);
            }
        }

        static async Task<ServerTask> UpdateAsync(ServerTask serverTask, Context context)
            => (await UpdatedAsync(new[] { serverTask }, context)).Single();
        static async Task<List<Task_<TAsset>>> UpdatedAsync(IEnumerable<Task_<TAsset>> tasks, Context context)
            => tasks.Zip(await UpdatedAsync(tasks.Select(_ => _._serverTask), context))
            .Select(_ => new { Task = _.First, ServerTask = _.Second })
            .Select(_ => Updated(_.Task, _.ServerTask))
            .ToList();
        static async Task<ServerTask[]> UpdatedAsync(IEnumerable<ServerTask> serverTasks, Context context)
        {
            if (serverTasks.Any())
            {
                var response = await
                    (await context.AssetsProcessor.PostAsJsonAsync(new Uri(TurboSquidApi._BaseUri, "turbosquid/uploads/bulk_poll"), new
                    {
                        context.Payload.authenticity_token,
                        ids = serverTasks.Select(_ => _.Id)
                    }, context.CancellationToken))
                    .EnsureSuccessStatusCode()
                    .Content.ReadAsStringAsync(context.CancellationToken);

                return JArray.Parse(response)
                    .Select(_ => _.ToObject<ServerTask>()!)
                    .ToArray();
            }
            else return Array.Empty<ServerTask>();
        }


        internal static async Task<List<ITurboSquidProcessed3DProductAsset<TAsset>>> WhenAll(List<Task_<TAsset>> tasks)
        {
            var tcs = new TaskCompletionSource<List<ITurboSquidProcessed3DProductAsset<TAsset>>>();
            var result = new List<ITurboSquidProcessed3DProductAsset<TAsset>>(tasks.Count);
            var exceptions = new List<Exception>();

            while (tasks.FirstOrDefault() is Task_<TAsset> task)
            {
                foreach (var updatedTask in await UpdatedAsync(tasks, task._context))
                    if (updatedTask._context.CancellationToken.IsCancellationRequested)
                    { tcs.SetCanceled(updatedTask._context.CancellationToken); break; }
                    else if (updatedTask._serverTask.IsCompletedSuccessfully)
                    { tasks.Remove(updatedTask); result.Add(TurboSquidProcessed3DProductAssetFactory.Create(updatedTask._asset, updatedTask._serverTask.FileId!)); }
                    else if (updatedTask._serverTask.IsFailed)
                    {
                        tasks.Remove(updatedTask);
                        tasks.Add(await updatedTask.RestartedAsync());
                    }

                if (tasks.Any())
                    if (task._context.CancellationToken.IsCancellationRequested)
                    { tcs.SetCanceled(task._context.CancellationToken); break; }
                await Delay(task._context.PollInterval, task._context.CancellationToken);
            }

            if (!tcs.TrySetResult(result))
                if (exceptions.Any())
                    tcs.TrySetException(exceptions);
            return await tcs.Task;
        }

        #region EqualityContract

        public override bool Equals(object? obj) => Equals(obj as Task_<TAsset>);
        public bool Equals(Task_<TAsset>? other) => Id == other?.Id;
        public override int GetHashCode() => Id.GetHashCode();

        #endregion

        class ServerTask : IEquatable<ServerTask>
        {
            [JsonProperty("id")]
            internal string Id { get; init; } = default!;

            internal bool IsCompletedSuccessfully => Status == "success";
            internal bool IsFailed => Status == "failed";

            [JsonProperty("status")]
            string Status { get; init; } = default!;

            [JsonProperty("file_id")]
            [MemberNotNullWhen(true, nameof(IsCompletedSuccessfully))]
            internal string? FileId { get; init; } = default!;

            #region EqualityContract

            public override bool Equals(object? obj) => Equals(obj as ServerTask);
            public bool Equals(ServerTask? other) => Id.Equals(other?.Id);
            public override int GetHashCode() => Id.GetHashCode();

            #endregion
        }

        record Context(Payload Payload, HttpClient AssetsProcessor, int PollInterval, CancellationToken CancellationToken)
        {
            internal Context(Payload payload, HttpClient assetsProcessor, CancellationToken cancellationToken)
                : this(payload, assetsProcessor, 1000, cancellationToken)
            {
            }
        }
    }
}

static class TurboSquid3DAssetsProcessingExtensions
{
    internal static async Task<List<Task_<TAsset>>> RunAsync<TAsset>(this IEnumerable<Task<Task_<TAsset>>> tasks)
        where TAsset : I3DProductAsset
        => (await Task.WhenAll(tasks)).ToList();
    // Conversion to array is required to actually launch tasks by iteration.
}
