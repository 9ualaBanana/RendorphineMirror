namespace Uploader;

public enum ProjectType { Release, Debug }
public record DotnetBuildAction(ProjectType Type, string Name, string RId, ImmutableArray<string> Dependencies = default, ImmutableArray<string> Args = default) : IAction
{
    readonly string Version = DateTime.UtcNow.ToString(@"yy\-MM\-d\-\UHHmm");
    public string PublishDir => GetBuildDir(Name);

    public void Invoke()
    {
        CheckProjectDir();
        Build(Version);
    }

    void CheckProjectDir()
    {
        var cwd = Directory.GetCurrentDirectory();
        Console.WriteLine($"Current directory: {cwd}");
        while (!Directory.GetFiles(cwd).Select(Path.GetExtension).Any(e => e == ".sln" || e == ".csproj"))
        {
            cwd = Path.GetDirectoryName(cwd);

            if (cwd is null) throw new InvalidOperationException("Could not find project directory (????????)");
            Console.WriteLine($"..is not a project directory, trying {cwd}");
        }

        if (!Directory.Exists(Path.Combine(cwd, Name)))
            throw new InvalidOperationException($"Specified project ({this}) does not exists");
    }

    string GetBuildDir(string name) => GetBuildDir(name, Type, RId);
    static string GetBuildDir(string name, ProjectType type, string identifier) => $"{name}/bin/{Enum.GetName(type)}/net8.0/{identifier}/publish/";

    public void Build(string version)
    {
        PublishProject(version, Name);

        if (!Dependencies.IsDefaultOrEmpty)
            foreach (var dep in Dependencies)
            {
                PublishProject(version, dep);
                CommonExtensions.MergeDirectories(GetBuildDir(dep), PublishDir);
            }
    }
    void PublishProject(string version, string name) =>
        new ProcessStartInfo("dotnet", $"publish {name} -c {Enum.GetName(Type)} -r {RId} /p:Version=\"{version}\" {string.Join(' ', Args.IsDefault ? ImmutableArray<string>.Empty : Args)}").StartProcessWait();
}
