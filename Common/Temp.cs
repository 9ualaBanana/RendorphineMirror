namespace Common;

public static class Temp
{
    public static string TempDir => Directories.Created(Directories.Data, "temp");

    public static string File(string name) => Path.Combine(TempDir, name);
}
