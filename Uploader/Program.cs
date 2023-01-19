using System.Collections.Immutable;
using System.Diagnostics;
using System.IO.Compression;
using Common;
using Newtonsoft.Json.Linq;
using UpdaterCommon;


var projects = new Project[]
{
    new NodeProject("renderfin-win", ProjectType.Release, "win7-x64", "debian@t.microstock.plus:/home/debian/updater3/files/renderfin-win"),
    new NodeProject("renderfin-lin", ProjectType.Release, "linux-x64", "debian@t.microstock.plus:/home/debian/updater3/files/renderfin-lin"),
    // new NodeProject("renderfin-osx", ProjectType.Release, "osx-x64", "debian@t.microstock.plus:/home/debian/updater3/files/renderfin-osx"),
}.ToImmutableArray();

if (args.Length != 0)
    projects = args
        .Where(a => a.StartsWith('{'))
        .Select(arg =>
        {
            var jobj = JObject.Parse(arg);
            var ctype = typeof(Project);

            var type = jobj.Property("projecttype", StringComparison.OrdinalIgnoreCase)?.Value?.Value<string>();
            if (type is not null)
                ctype = new[] { typeof(Project), typeof(NodeProject) }.First(t => t.Name.Equals(type, StringComparison.OrdinalIgnoreCase));

            var project = (Project) jobj.ToObject(ctype).ThrowIfNull("Could not deserialize project " + jobj);
            return project;
        })
        .ToImmutableArray();


var cwd = Directory.GetCurrentDirectory();
Console.WriteLine($"Current directory: {cwd}");
while (!Directory.GetFiles(cwd).Contains(Path.Combine(cwd, "RenderphineNode.sln")))
{
    cwd = Path.GetDirectoryName(cwd);

    if (cwd is null) throw new InvalidOperationException("Could not find project directory (????????)");
    Console.WriteLine($"..is not a project directory, trying {cwd}");
}

foreach (var project in projects)
    if (!Directory.Exists(Path.Combine(cwd, project.Name)))
        throw new InvalidOperationException($"Specified project ({project}) does not exists");


var version = DateTime.UtcNow.ToString(@"yy\-MM\-d\-\UHHmm"); // 22.12.2-U1014

Console.WriteLine($"App version: {version}");
Console.WriteLine($"Projects:\n {string.Join("\n ", projects.AsEnumerable())}");

foreach (var project in projects) project.Publish(version);
projects.AsParallel().ForAll(project =>
{
    project.InBetween();
    project.Upload();
});

Console.WriteLine("Done.");


enum ProjectType { Release, Debug }
record Project(ProjectType Type, string Name, string Identifier, string Dest, bool Compressed = false, ImmutableArray<string> Dependencies = default, ImmutableArray<string> Args = default)
{
    public string PublishDir => GetPublishDir(Name);

    public string GetPublishDir(string name) => GetPublishDir(name, Type, Identifier);
    public static string GetPublishDir(string name, ProjectType type, string identifier) => $"{name}/bin/{Enum.GetName(type)}/net6.0/{identifier}/publish/";

    public void Publish(string version)
    {
        PublishDependency(version, Name);

        if (!Dependencies.IsDefaultOrEmpty)
            foreach (var dep in Dependencies)
            {
                PublishDependency(version, dep);
                CommonExtensions.MergeDirectories(GetPublishDir(dep), PublishDir);
            }
    }
    void PublishDependency(string version, string name) =>
        StartProcessWait(new ProcessStartInfo("dotnet", $"publish {name} -c {Enum.GetName(Type)} -r {Identifier} /p:Version=\"{version}\" {string.Join(' ', Args.IsDefault ? ImmutableArray<string>.Empty : Args)}"));


    protected static void WriteLine(ConsoleColor fg, string text)
    {
        var prevcolor = Console.ForegroundColor;
        Console.ForegroundColor = fg;
        Console.WriteLine(text);
        Console.ForegroundColor = prevcolor;
    }
    protected static Process StartProcess(ProcessStartInfo info)
    {
        WriteLine(ConsoleColor.Green, $"Starting {info.FileName} {info.Arguments} {string.Join(' ', info.ArgumentList)}");
        return Process.Start(info) ?? throw new Exception($"Could not start {info.FileName}");
    }
    protected static void StartProcessWait(ProcessStartInfo info)
    {
        var proc = StartProcess(info);
        proc.WaitForExit();

        if (proc.ExitCode != 0) throw new Exception($"Non-zero exit code from {info.FileName}: {proc.ExitCode}");
    }

    protected static void Upload(string source, string dest)
    {
        if (File.Exists("/bin/rsync"))
            Start("/bin/rsync", "-ravhP", "-T=/tmp", source, dest);
        else Start("scp", "-pr", source, dest);
    }
    protected static void Start(string exe, params string[] args)
    {
        var pinfo = new ProcessStartInfo(exe);
        foreach (var arg in args)
            pinfo.ArgumentList.Add(arg);

        StartProcessWait(pinfo);
    }

    public virtual void Upload() => Upload(PublishDir, Dest);
    public virtual void InBetween() { }
}
record NodeProject(string AppType, ProjectType Type, string Identifier, string Dest, ImmutableArray<string> Args = default) : Project(Type, "Node", Identifier, Dest, true, ImmutableArray.Create("Updater"), Args: Args)
{
    public override void InBetween()
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
    public override void Upload()
    {
        var source = PublishDir;
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

        WriteLine(ConsoleColor.Green, $"[{AppType}] Found {differentall.Length} different files");
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

        Upload(tempdir, Dest);
        // TODO: delete oldfiles stuff from server
    }
}