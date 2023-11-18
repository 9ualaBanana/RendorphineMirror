namespace Common;

public static class Directories
{
    public static string NumberedNameInDirectory(string dir, string format)
    {
        var num = 0;
        string path;
        do
        {
            path = Path.Combine(dir, string.Format(CultureInfo.InvariantCulture, format, num));
            num++;
        }
        while (File.Exists(path) || Directory.Exists(path));

        return path;
    }
    public static string RandomNameInDirectory(string dir, string? extension = null)
    {
        string path;
        do { path = Path.Combine(dir, Guid.NewGuid().ToString()); }
        while (File.Exists(path) || Directory.Exists(path));

        if (extension is not null)
            return path + (extension.StartsWith('.') ? extension : ($".{extension}"));

        return path;
    }

    public static FuncDispose DisposeDelete(string path, out string samepath)
    {
        samepath = path;
        return DisposeDelete(path);
    }

    public static FuncDispose DisposeDelete(params string[] paths) => DisposeDelete<string[]>(paths, out _);
    public static FuncDispose DisposeDelete<TPaths>(TPaths paths, out TPaths samepaths)
        where TPaths : IReadOnlyCollection<string>
    {
        samepaths = paths;
        return new FuncDispose(delete);


        void delete()
        {
            foreach (var path in paths)
            {
                try
                {
                    if (File.Exists(path)) File.Delete(path);
                    if (Directory.Exists(path)) Directory.Delete(path);
                }
                catch { }
            }
        }
    }


    public static string DirCreated(bool create, params string[] parts) => create ? DirCreated(parts) : Path.GetFullPath(Path.Combine(parts));
    public static string FileCreated(bool create, params string[] parts) => create ? FileCreated(parts) : Path.GetFullPath(Path.Combine(parts));

    public static string FileCreated(params string[] parts) =>
        FullPath(parts)
            .With(path => Directory.CreateDirectory(Path.GetDirectoryName(path).ThrowIfNull()))
            .With(path => File.Create(path).Dispose());
    public static string DirCreated(params string[] parts) =>
        FullPath(parts)
            .With(path => Directory.CreateDirectory(path));

    public static string NewFileCreated(params string[] parts) =>
        FullPath(parts)
            .With(path => { if (File.Exists(path)) File.Delete(path); })
            .With(path => Directory.CreateDirectory(Path.GetDirectoryName(path).ThrowIfNull()))
            .With(path => File.Create(path).Dispose());
    public static string NewDirCreated(params string[] parts) =>
        FullPath(parts)
            .With(path => { if (Directory.Exists(path)) Directory.Delete(path, true); })
            .With(path => Directory.CreateDirectory(path));

    static string FullPath(params string[] parts) => Path.GetFullPath(Path.Combine(parts));


    static void ForEachEntry(string source, string destination, Action<string, string> func)
    {
        source = Path.GetFullPath(source);
        destination = Path.GetFullPath(destination);

        Directory.GetDirectories(source, "*", SearchOption.AllDirectories).AsParallel().ForAll(x => Directory.CreateDirectory(x.Replace(source, destination)));
        Directory.GetFiles(source, "*", SearchOption.AllDirectories).AsParallel().ForAll(x => func(x, x.Replace(source, destination)));
    }

    /// <summary> Copy a directory </summary>
    public static void Copy(string source, string destination)
    {
        Directory.CreateDirectory(destination);
        ForEachEntry(source, destination, (s, d) => File.Copy(s, d, true));
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
        ForEachEntry(source, destination, (s, d) => File.Move(s, d, true));
        Directory.Delete(source, true);
    }
}
