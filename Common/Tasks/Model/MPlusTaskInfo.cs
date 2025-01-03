namespace Common.Tasks.Model;

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
        var datar = await Api.ApiPost<ImmutableDictionary<string, MPlusNewItem>>($"{Api.TaskManagerEndpoint}/getmpitems", "items", "Getting mp item info", ("sessionid", Settings.SessionId), ("userid", TUid ?? Settings.UserId), ("iids", $"[\"{Iid}\"]"));
        var data = datar.ThrowIfError()[Iid];

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

    public readonly string? TUid;
    [Hidden] public string? IngesterHost;

    public MPlusTaskOutputInfo(string name, string directory, string? tuid = null)
    {
        Name = name;
        Directory = directory;
        TUid = tuid;
    }
}