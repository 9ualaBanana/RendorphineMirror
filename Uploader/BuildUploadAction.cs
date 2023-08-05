namespace Uploader;

public record BuildUploadAction(ProjectType Type, string Name, string RId, string Destination, ImmutableArray<string> Dependencies = default, ImmutableArray<string> Args = default) : IAction
{
    public void Invoke()
    {
        var build = new DotnetBuildAction(Type, Name, RId, Dependencies, Args);
        build.Invoke();

        new UploadAction(build.PublishDir, Destination).Invoke();
    }
}
