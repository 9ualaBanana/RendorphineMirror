using Node.Tasks.Models.ExecInfo;

namespace Node.Tasks.Watching.Handlers.Input;

public class QSWatchingTaskHandler : MPlusWatchingTaskHandlerBase<MPlusAllFilesWatchingTaskInputInfo>, ITypedTaskWatchingInput
{
    public static WatchingTaskInputType Type => WatchingTaskInputType.MPlusAllFiles;
    readonly HashSet<string> ProcessedIids = new();

    protected override async Task<OperationResult<ImmutableArray<MPlusNewItem>>> FetchItemsAsync()
    {
        var qwertykey = File.ReadAllText("qwertykey").Trim();
        var mpluskey = File.ReadAllText("mpluskey").Trim();

        Logger.Info($"Fetching items (updatingfornewversion: {Input.IsUpdatingForNewVersion})");
        return await process(getUsers().Next(users => getQSItems(users)))
            .Next(async files =>
            {
                var isnewversion = files.Any(f => f.QSPreviewVersion != IO.Handlers.Output.QSPreview.Version);
                Logger.Info($"Fetched {files.Length} items (isnewversion: {isnewversion}, updatingfornewversion: {Input.IsUpdatingForNewVersion})");

                if (!Input.IsUpdatingForNewVersion && isnewversion)
                {
                    return await NotifyQSPreviewVersion(true)
                        .Next(() =>
                        {
                            Input.IsUpdatingForNewVersion = true;
                            SaveTask();
                            return files.AsOpResult();
                        });
                }

                return files.AsOpResult();
            });


        Task<OperationResult<ImmutableArray<MPlusNewItem>>> process(Task<OperationResult<ImmutableArray<QwertyStockItem>>> qitems) =>
            qitems
            .Next(qitems =>
            {
                var items = qitems.Where(qitem => !ProcessedIids.Contains(qitem.Iid)).ToArray();
                ProcessedIids.Clear();
                foreach (var item in qitems)
                    ProcessedIids.Add(item.Iid);

                return items.AsOpResult();
            })
            .Next(qitems => qitems.GroupBy(i => i.UserId).AsOpResult())
            .Next(qitems => qitems.Select(async i => await getMPItems(i.Key, i.Select(i => i.Iid))).Aggregate())
            .Next(result => result.SelectMany(i => i.Values).ToImmutableArray().AsOpResult());

        async Task<OperationResult<ImmutableArray<string>>> getUsers() =>
            await Api.ApiGet<ImmutableArray<string>>($"{Api.ContentDBEndpoint}/users/getqwertystockusers", "users", "Getting sale content without preview",
                Api.SignRequest(qwertykey, ("timestamp", DateTimeOffset.Now.ToUnixTimeMilliseconds().ToString())));

        async Task<OperationResult<ImmutableArray<QwertyStockItem>>> getQSItems(IEnumerable<string>? userids)
        {
            userids = userids?.Except(Input.NonexistentUsers);

            (string, string)[] data;
            if (userids is null)
                data = new[]
                {
                    ("timestamp", DateTimeOffset.Now.ToUnixTimeMilliseconds().ToString()),
                    ("minver", IO.Handlers.Output.QSPreview.Version.ToStringInvariant())
                };
            else
                data = new[]
                {
                    ("timestamp", DateTimeOffset.Now.ToUnixTimeMilliseconds().ToString()),
                    ("userids", JsonConvert.SerializeObject(userids)),
                    ("minver", IO.Handlers.Output.QSPreview.Version.ToStringInvariant())
                };

            return await Api.ApiGet<ImmutableArray<QwertyStockItem>>(
                $"{Api.ContentDBEndpoint}/content/getonsalewithoutpv", "list", "Getting sale content without preview",
                Api.SignRequest(qwertykey, data)
            );
        }

        ValueTask<OperationResult<ImmutableDictionary<string, MPlusNewItem>>> getMPItems(string userid, IEnumerable<string> iids) =>
            Api.ApiPost<ImmutableDictionary<string, MPlusNewItem>>($"{Api.ContentDBEndpoint}/content/getitems", "items", "Getting m+ items info",
                Api.SignRequest(mpluskey, ("userid", userid), ("iids", JsonConvert.SerializeObject(iids))));
    }
    async Task<OperationResult> NotifyQSPreviewVersion(bool updatingFiles) =>
        await Api.ApiPost($"https://qwertystock.com/search/notifypreviewversion", "Notifying QS about preview version updates",
            Api.SignRequest(File.ReadAllText("qwertykey").Trim(), ("version", IO.Handlers.Output.QSPreview.Version.ToStringInvariant()), ("isneedupdate", "false"), ("isfinished", (!updatingFiles).ToString()))
        );


    public override void OnCompleted(DbTaskFullState task)
    {
        base.OnCompleted(task);

        if (Input.IsUpdatingForNewVersion && Task.PlacedNonCompletedTasks.Count == 0)
        {
            NotifyQSPreviewVersion(false)
                .Next(() =>
                {
                    Input.IsUpdatingForNewVersion = false;
                    SaveTask();
                    return OperationResult.Succ();
                })
                .ThrowIfError()
                .Consume();
        }
    }

    protected override async Task Tick()
    {
        // fetch new items only if there is less than N ptasks pending
        const int taskFetchingThreshold = 500 - 1;

        if (Task.PlacedNonCompletedTasks.Count > taskFetchingThreshold)
            return;

        Logger.LogInformation($"Found {Task.PlacedNonCompletedTasks.Count} unfinished ptasks, fetching new items");
        await base.Tick();
    }

    protected override async Task<DbTaskFullState> Register(MPlusTaskInputInfo input, ITaskOutputInfo output, TaskObject tobj)
    {
        var qid = await Api.ApiGet<QwertyFileIdResult>($"https://qwertystock.com/search/getiids", null, "getting qs file id",
            Api.SignRequest(File.ReadAllText("qwertykey").Trim(), ("mpiids", JsonConvert.SerializeObject(new[] { input.Iid }))))
            .ThrowIfError();

        var data = new QSPreviewInfo(qid.Result[input.Iid].ToStringInvariant());
        return await TaskRegistration.RegisterAsync(Task, input, output, tobj, data);
    }

    record QwertyFileIdResult(
        // <mpiid, qid>
        ImmutableDictionary<string, ulong> Result
    );

    record QwertyStockItem(string UserId, string Iid);
}