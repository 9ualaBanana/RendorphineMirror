namespace Common;

public static class Directories
{
    public static string Created(params string[] parts) => Created(Path.Combine(parts));
    public static string Created(string path)
    {
        try { Directory.CreateDirectory(path); }
        catch { }

        return path;
    }
}
