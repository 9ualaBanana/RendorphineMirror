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

    public ValueTask<OperationResult<TaskObject>> GetFileInfo(string sessionid, string myuserid) => GetFileInfo(sessionid, TUid ?? myuserid, Iid);
    public static ValueTask<OperationResult<TaskObject>> GetFileInfo(string sessionid, string userid, string iid)
    {
        var get = () => Api.Default.ApiPost<ImmutableDictionary<string, MPlusNewItem>>($"{Api.TaskManagerEndpoint}/getmpitems", "items", "Getting mp item info",
            ("sessionid", sessionid), ("userid", userid), ("iids", $"[\"{userid}\"]"));

        return get().Next(data => new TaskObject(data[iid].Files.File.FileName, data[iid].Files.File.Size).AsOpResult());
    }
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