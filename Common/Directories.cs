namespace Common;

public static class Directories
{
    static Directories() => Directory.Delete(Temp(), true);


    /// <summary> Application data; %appdata%/{appname} or ~/.config/{appname} </summary>
    public static readonly string Data = DataFor(Initializer.AppName);
    public static string DataFor(string appname) => Created(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData, Environment.SpecialFolderOption.Create), appname);

    public static string DataDir(params string[] subdirs) => Created(new[] { Data }.Concat(subdirs).ToArray());
    public static string DataFile(string file) => Path.Combine(Data, file);


    /// <summary> Temp directory; {<see cref="Data"/>}/temp/[...<paramref name="subdirs"/>]. Cleaned every launch </summary>
    public static string Temp(params string[] subdirs) => Created(Data, "temp", Path.Combine(subdirs));

    /// <summary> Temp directory; {<see cref="Data"/>}/temp/[...<paramref name="subdirs"/>]. Cleaned every launch </summary>
    public static FuncDispose TempDispose(out string dir, params string[] subdirs)
    {
        dir = Created(Data, "temp", Path.Combine(subdirs));
        return DisposeDelete(dir);
    }

    /// <summary> Temp file; {<see cref="Data"/>}/temp/[...<paramref name="subdirs"/>]/{randomname} </summary>
    /// <returns> Struct that will delete the file when disposed </returns>
    public static FuncDispose TempFile(out string tempfile, params string[] subdirs)
    {
        do { tempfile = Path.Combine(Temp(subdirs), Guid.NewGuid().ToString()); }
        while (File.Exists(tempfile));

        var delfile = tempfile;
        return new FuncDispose(() =>
        {
            try
            {
                if (File.Exists(delfile))
                    File.Delete(delfile);
            }
            catch { }
        });
    }

    /// <summary> Temp files; {<see cref="Data"/>}/temp/[...<paramref name="subdirs"/>]/{randomname} * <paramref name="count"/> </summary>
    /// <returns> Struct that will delete the files when disposed </returns>
    public static FuncDispose TempFiles(int count, out IReadOnlyList<string> tempfiles, params string[] subdirs)
    {
        var disposes = new List<FuncDispose>();
        var files = new List<string>();

        for (int i = 0; i < count; i++)
        {
            disposes.Add(TempFile(out var file, subdirs));
            files.Add(file);
        }

        tempfiles = files;
        return FuncDispose.Create(disposes);
    }


    /// <summary> Deletes files or directories when disposed </summary>
    public static FuncDispose DisposeDelete(params string[] paths) => new FuncDispose(() =>
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
