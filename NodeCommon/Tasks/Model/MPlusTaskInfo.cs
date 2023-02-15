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

    public async ValueTask<TaskObject> GetFileInfo()
    {
        var datar = await Api.Default.ApiPost<ImmutableDictionary<string, MPlusNewItem>>($"{Api.TaskManagerEndpoint}/getmpitems", "items", "Getting mp item info", ("sessionid", Settings.SessionId), ("userid", TUid ?? Settings.UserId), ("iids", $"[\"{Iid}\"]"));
        var data = datar.ThrowIfError()[Iid];

        return new TaskObject(data.Files.File.FileName, data.Files.File.Size);
    }
    public static async ValueTask<TaskObject> GetFileInfo(string iid, string userid, string sessionid)
    {
        var datar = await Api.Default.ApiPost<ImmutableDictionary<string, MPlusNewItem>>($"{Api.TaskManagerEndpoint}/getmpitems", "items", "Getting mp item info", ("sessionid", sessionid), ("userid", userid), ("iids", $"[\"{userid}\"]"));
        var data = datar.ThrowIfError()[iid];

        return new TaskObject(data.Files.File.FileName, data.Files.File.Size);
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