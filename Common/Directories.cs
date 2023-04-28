namespace Common;

public static class Directories
{
    static Directories() => Directory.Delete(Temp(), true);


    /// <summary> Application data; %appdata%/{appname} or ~/.config/{appname} </summary>
    public static string Data = Created(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData, Environment.SpecialFolderOption.Create), Initializer.AppName);

    /// <summary> Temp directory; {datadir}/temp/[...subdirs]. Cleaned every launch </summary>
    public static string Temp(params string[] subdirs) => Created(new[] { Data, "temp" }.Concat(subdirs).ToArray());


    /// <summary> Deletes a file or directory when disposed </summary>
    public static FuncDispose DisposeDelete(string path) => new FuncDispose(() =>
    {
        if (File.Exists(path)) File.Delete(path);
        if (Directory.Exists(path)) Directory.Delete(path);
    });



    /// <inheritdoc cref="Created"/>
    public static string Created(params string[] parts) => Created(Path.Combine(parts));

    /// <summary> Creates a directory and return its full path </summary>
    public static string Created(string path) => Directory.CreateDirectory(path).FullName;

    /// <inheritdoc cref="CreatedNew"/>
    public static string CreatedNew(params string[] parts) => CreatedNew(Path.Combine(parts));

    /// <summary> Deletes a directory if exists, then creates a directory and return its full path </summary>
    public static string CreatedNew(string path)
    {
        if (Directory.Exists(path))
            Directory.Delete(path, true);

        return Created(path);
    }


    static void ForEachFile(string source, string destination, Action<string, string> func)
    {
        source = Path.GetFullPath(source);
        destination = Path.GetFullPath(destination);

        Directory.GetDirectories(source, "*", SearchOption.AllDirectories).AsParallel().ForAll(x => Directory.CreateDirectory(x.Replace(source, destination)));
        Directory.GetFiles(source, "*", SearchOption.AllDirectories).AsParallel().ForAll(x => func(x, x.Replace(source, destination)));
    }

    /// <summary> Copy a directory </summary>
    public static void Copy(string source, string destination)
    {
        Created(destination);
        ForEachFile(source, destination, (s, d) => File.Copy(s, d, true));
    }

    /// <summary>
    /// Merge <paramref name="source"/> into <paramref name="destination"/>, then delete <paramref name="source"/> directory.
    /// <example>
    /// Example:
    /// <code>
    ///     dir1: file1, file2, file3
    ///     dir2: file3, file4
    /// </code>
    /// With Merge("dir1", "dir2") dir2 becomes
    /// <code>
    ///     dir2: file1, file2, file3 (the one from dir1), file4
    /// </code>
    /// </example>
    /// </summary>
    public static void Merge(string source, string destination)
    {
        ForEachFile(source, destination, (s, d) => File.Move(s, d, true));
        Directory.Delete(source, true);
    }
}
