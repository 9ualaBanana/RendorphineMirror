using static Common.Directories;

namespace Node.Common;

public class DataDirs
{
    public string Data { get; }
    public string Temp { get; }
    readonly string AppName;

    public DataDirs(string appname) : this(new Init.InitConfig(appname)) { }
    public DataDirs(Init.InitConfig config)
    {
        AppName = config.AppName;

        Data = DirCreated(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData, Environment.SpecialFolderOption.Create), AppName);

        if (config.AutoClearTempDir)
        {
            try { Temp = NewDirCreated(Data, "temp"); }
            catch { Temp = DirCreated("temp"); }
        }
        else Temp = DirCreated("temp");
    }

    public string DataDir(string name, bool create = true) => DirCreated(create, Data, name);
    public string DataFile(string name, bool create = false) => FileCreated(create, Data, name);

    public string TempDir(string parentdir = "", bool create = true)
    {
        parentdir = DirCreated(Temp, parentdir);
        return DirCreated(create, parentdir, RandomNameInDirectory(parentdir));
    }
    public string TempFile(string parentdir = "", bool create = false)
    {
        parentdir = DirCreated(Temp, parentdir);
        return FileCreated(create, parentdir, RandomNameInDirectory(parentdir));
    }

    public string NamedTempDir(string name, bool create = true) => DirCreated(create, Temp, name);
    public string NamedTempFile(string name, bool create = false) => FileCreated(create, Temp, name);

    public ImmutableArray<string> TempFiles(int count, string parentdir = "", bool create = false) =>
        Enumerable.Range(0, count)
            .Select(_ => TempFile(parentdir, create))
            .ToImmutableArray();
}
