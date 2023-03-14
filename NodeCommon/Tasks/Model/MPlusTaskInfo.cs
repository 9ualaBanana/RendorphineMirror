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


    static ValueTask<OperationResult<ImmutableDictionary<string, MPlusNewItem>>> GetMpItems(string sessionid, string userid, IEnumerable<string> iids) =>
        Api.Default.ApiPost<ImmutableDictionary<string, MPlusNewItem>>($"{Api.TaskManagerEndpoint}/getmpitems", "items", "Getting mp item info",
            ("sessionid", sessionid), ("userid", userid), ("iids", JsonConvert.SerializeObject(iids)));

    public ValueTask<OperationResult<TaskObject>> GetFileInfo(string sessionid, string myuserid) => GetFileInfo(sessionid, TUid ?? myuserid, Iid);
    public static ValueTask<OperationResult<TaskObject>> GetFileInfo(string sessionid, string userid, string iid) =>
        GetMpItems(sessionid, userid, new[] { iid }).Next(data => new TaskObject(data[iid].Files.File.FileName, data[iid].Files.File.Size).AsOpResult());
    public static ValueTask<OperationResult<Dictionary<string, TaskObject>>> GetFilesInfoDict(string sessionid, string userid, IEnumerable<string> iids) =>
        GetMpItems(sessionid, userid,iids).Next(data => data.Values.Select(item => KeyValuePair.Create(item.Iid, new TaskObject(item.Files.File.FileName, item.Files.File.Size)).AsOpResult()).MergeDictResults());
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

    public MPlusTaskOutputInfo(string name, string directory, int? autoremoveTimer = null, string? tuid = null)
    {
        Name = name;
        Directory = directory;
        AutoremoveTimer = autoremoveTimer;
        TUid = tuid;
    }
}