using static Common.Directories;

namespace Node.Common;

public class DataDirs
{
    public string Data { get; }
    public string Temp { get; }
    readonly string AppName;

    public DataDirs(Init.InitConfig config) : this(config.AppName) { }
    public DataDirs(string appname)
    {
        AppName = appname;

        Data = DirCreated(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData, Environment.SpecialFolderOption.Create), AppName);
        Temp = NewDirCreated(Data, "temp");
    }

    public string DataDir(string name, bool create = true) => DirCreated(create, Data, name);
    public string DataFile(string name, bool create = false) => FileCreated(create, Data, name);

    public string TempDir(string parentdir = "", bool create = true, string? extension = null)
    {
        parentdir = DirCreated(Temp, parentdir);
        return DirCreated(create, parentdir, RandomNameInDirectory(parentdir, extension));
    }
    public string TempFile(string parentdir = "", bool create = false, string? extension = null)
    {
        parentdir = DirCreated(Temp, parentdir);
        return FileCreated(create, parentdir, RandomNameInDirectory(parentdir, extension));
    }

    public string NamedTempDir(string name, bool create = true) => DirCreated(create, Temp, name);
    public string NamedTempFile(string name, bool create = false) => FileCreated(create, Temp, name);

    public ImmutableArray<string> TempFiles(int count, string parentdir = "", bool create = false) =>
        Enumerable.Range(0, count)
            .Select(_ => TempFile(parentdir, create))
            .ToImmutableArray();
}
