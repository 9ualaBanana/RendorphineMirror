using Newtonsoft.Json;

namespace NodeCommon.Tasks.Model;

public class MPlusTaskInputInfo : ITaskInputInfo
{
    public TaskInputType Type => TaskInputType.MPlus;

    public readonly string Iid;
    public readonly string? TUid;

    public MPlusTaskInputInfo(string iid, string? tuid = null)
    {
        Iid = iid;
        TUid = tuid;
    }


    static Task<OperationResult<Dictionary<string, MPlusNewItem>>> GetMpItems(Api api, string sessionid, string userid, IEnumerable<string> iids) =>
        iids.Chunk(30).Select(async iids =>
            await api.ApiPost<Dictionary<string, MPlusNewItem>>($"{Api.TaskManagerEndpoint}/getmpitems", "items", "Getting mp item info",
                ("sessionid", sessionid), ("userid", userid), ("iids", JsonConvert.SerializeObject(iids)))
        )
        .AggregateMany();

    public Task<OperationResult<TaskObject>> GetFileInfo(Api api, string sessionid, string myuserid) => GetFileInfo(api, sessionid, TUid ?? myuserid, Iid);
    public static Task<OperationResult<TaskObject>> GetFileInfo(Api api, string sessionid, string userid, string iid) =>
        GetMpItems(api, sessionid, userid, new[] { iid }).Next(data => new TaskObject(data[iid].Files.File.FileName, data[iid].Files.File.Size).AsOpResult());
    public static Task<OperationResult<Dictionary<string, TaskObject>>> GetFilesInfoDict(Api api, string sessionid, string userid, IEnumerable<string> iids) =>
        GetMpItems(api, sessionid, userid, iids).Next(data => data.Values.Select(item => KeyValuePair.Create(item.Iid, new TaskObject(item.Files.File.FileName, item.Files.File.Size)).AsOpResult()).Aggregate());
}
public class MPlusTaskOutputInfo : ITaskOutputInfo
{
    public TaskOutputType Type => TaskOutputType.MPlus;

    [Default("output_file.mov")]
    public readonly string Name;

    [Default("output_dir")]
    public readonly string Directory;

    public readonly int? AutoremoveTimer;
    public readonly string? TUid;
    [Hidden] public string? IngesterHost;
    public string? CustomHost { get; init; }

    public MPlusTaskOutputInfo(string name, string directory, int? autoremoveTimer = null, string? tuid = null)
    {
        Name = name;
        Directory = directory;
        AutoremoveTimer = autoremoveTimer;
        TUid = tuid;
    }
}
