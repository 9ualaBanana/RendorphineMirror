using Newtonsoft.Json;

namespace Node.Tasks.Watching;

public class MPlusAllFilesWatchingTaskHandler : MPlusWatchingTaskHandler<MPlusAllFilesWatchingTaskInputInfo>
{
    public override WatchingTaskInputType Type => WatchingTaskInputType.MPlusAllFiles;
    readonly HashSet<string> ProcessedIids = new();
    public readonly HashSet<string> NonexistentUsers = new();
    int Index = -1;

    public MPlusAllFilesWatchingTaskHandler(WatchingTask task) : base(task) { }

    protected override ValueTask<OperationResult<ImmutableArray<MPlusNewItem>>> FetchItemsAsync()
    {
        var qwertykey = File.ReadAllText("qwertykey").Trim();
        var mpluskey = File.ReadAllText("mpluskey").Trim();

        Index++;

        ValueTask<OperationResult<ImmutableArray<QwertyStockItem>>> qitems;
        if (Index % 10 == 0) qitems = getUsers().Next(users => getQSItems(users));
        else qitems = getQSItems(null);

        return qitems
            .Next(qitems =>
            {
                var items = qitems.Where(qitem => !ProcessedIids.Contains(qitem.Iid)).ToArray();
                ProcessedIids.Clear();
                foreach (var item in qitems)
                    ProcessedIids.Add(item.Iid);

                return items.AsOpResult();
            })
            .Next(qitems => qitems.GroupBy(i => i.UserId).AsOpResult())
            .Next(qitems => qitems.Select(async i => await getMPItems(i.Key, i.Select(i => i.Iid))).MergeResults())
            .Next(result => result.SelectMany(i => i.Values).ToImmutableArray().AsOpResult());


        ValueTask<OperationResult<ImmutableArray<string>>> getUsers() =>
            Api.Default.ApiGet<ImmutableArray<string>>($"{Api.ContentDBEndpoint}/users/getqwertystockusers", "users", "Getting sale content without preview",
                Api.SignRequest(qwertykey, ("timestamp", DateTimeOffset.Now.ToUnixTimeMilliseconds().ToString())));

        ValueTask<OperationResult<ImmutableArray<QwertyStockItem>>> getQSItems(IEnumerable<string>? userids)
        {
            userids = userids?.Except(NonexistentUsers);

            (string, string)[] data;
            if (userids is null)
                data = new[]
                {
                    ("timestamp", DateTimeOffset.Now.ToUnixTimeMilliseconds().ToString()),
                    ("minver", QSPreviewTaskHandler.Version)
                };
            else
                data = new[]
                {
                    ("timestamp", DateTimeOffset.Now.ToUnixTimeMilliseconds().ToString()),
                    ("userids", JsonConvert.SerializeObject(userids)),
                    ("minver", QSPreviewTaskHandler.Version)
                };

            return Api.Default.ApiGet<ImmutableArray<QwertyStockItem>>(
                $"{Api.ContentDBEndpoint}/content/getonsalewithoutpv", "list", "Getting sale content without preview",
                Api.SignRequest(qwertykey, data)
            );
        }

        ValueTask<OperationResult<ImmutableDictionary<string, MPlusNewItem>>> getMPItems(string userid, IEnumerable<string> iids) =>
            Api.Default.ApiPost<ImmutableDictionary<string, MPlusNewItem>>($"{Api.ContentDBEndpoint}/content/getitems", "items", "Getting m+ items info",
                Api.SignRequest(mpluskey, ("userid", userid), ("iids", JsonConvert.SerializeObject(iids))));
    }

    protected override async ValueTask Tick()
    {
        // fetch new items only if there is less than N ptasks pending
        const int taskFetchingThreshold = 500 - 1;

        if (Task.PlacedNonCompletedTasks.Count > taskFetchingThreshold)
            return;

        Task.LogInfo($"Found {Task.PlacedNonCompletedTasks.Count} unfinished ptasks, fetching new items");
        await base.Tick();
    }


    record QwertyStockItem(string UserId, string Iid);
}