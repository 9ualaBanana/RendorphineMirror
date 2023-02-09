namespace Uploader;

public record BuildUploadNodeAction(string AppType, ProjectType Type, string Identifier, string Dest, ImmutableArray<string> Args = default) : IAction
{
    readonly DotnetBuildAction BuildAction = new DotnetBuildAction(Type, "Node", Identifier, ImmutableArray.Create("Updater"), Args: Args);

    public void Invoke()
    {
        BuildAction.Invoke();

        EditBin();
        Upload();
    }

    void EditBin()
    {
        // workaround for https://github.com/dotnet/runtime/issues/3828
        if (Identifier.Contains("win", StringComparison.OrdinalIgnoreCase) && Environment.OSVersion.Platform == PlatformID.Unix)
        {
            var winexes = Directory.GetFiles("Node/bin/Release/", "*.exe", SearchOption.AllDirectories)
                .Where(e => !e.EndsWith("Updater.exe", StringComparison.OrdinalIgnoreCase));

            using var proc = Process.Start(new ProcessStartInfo("wine", $"editbin/editbin.exe /subsystem:windows {string.Join(' ', winexes)}"))!;
            proc.WaitForExit();
        }
    }
    void Upload()
    {
        var source = BuildAction.PublishDir;
        var allfiles = new UpdateChecker(app: AppType).GetAllFiles().ThrowIfError().AsTask().GetAwaiter().GetResult();
        var different = UpdateChecker.FilterNewFiles(new[] { source }, allfiles);

        var tocompress = new List<string>();
        var todel = new List<string>();

        // only files that are different or new
        var differentall = different.Select(x => Path.GetFullPath(Path.Combine(source, x.Path)))
            .Concat(Directory.GetFiles(source, "*", SearchOption.AllDirectories)
                .Where(x => !allfiles.Any(y => Path.GetFullPath(Path.Combine(source, y.Path)) == Path.GetFullPath(x)))
            )
            .Distinct()
            .ToArray();

        if (differentall.Length == 0) return;

        ConsoleColor.Green.WriteLine($"[{AppType}] Found {differentall.Length} different files");
        foreach (var path in differentall)
        {
            if (File.Exists(path))
                tocompress.Add(path);
            else if (Directory.Exists(Path.GetDirectoryName(path)))
                todel.Add(path);
        }

        var tempdir = Path.Combine(Path.GetTempPath(), "uuploader" + AppType) + "/";
        if (Directory.Exists(tempdir)) Directory.Delete(tempdir, true);
        Directory.CreateDirectory(tempdir);
        using var _ = new FuncDispose(() => Directory.Delete(tempdir, true));

        Parallel.ForEach(tocompress, file =>
        {
            var tempfile = Path.Combine(tempdir, Path.GetRelativePath(source, file));
            Directory.CreateDirectory(Path.GetDirectoryName(tempfile)!);

            using var tempfs = File.OpenWrite(tempfile);
            using var gzip = new GZipStream(tempfs, CompressionLevel.SmallestSize);

            using var fs = File.OpenRead(file);
            fs.CopyTo(gzip);
            Console.WriteLine($"[{AppType}] Zipped {Path.GetFileName(file)}");
        });

        UploadAction.Upload(tempdir, Dest + "/" + AppType);
        // TODO: delete oldfiles stuff from server
    }
}
