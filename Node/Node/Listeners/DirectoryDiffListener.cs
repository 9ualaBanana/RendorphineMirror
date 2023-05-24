using System.Net;

namespace Node.Listeners;

public class DirectoryDiffListener : ExecutableListenerBase
{
    protected override ListenTypes ListenType => ListenTypes.Public;
    protected override bool RequiresAuthentication => true;
    protected override string? Prefix => "dirdiff";


    protected override async Task<HttpStatusCode> ExecuteGet(string path, HttpListenerContext context)
    {
        // TODO: whitelist for directories or something?

        var response = context.Response;

        var values = ReadQueryString(context.Request.QueryString, "path")
            .Next(dir => ReadQueryLong(context.Request.QueryString, "lastcheck", 0)
            .Next(lastcheck => (dir, lastcheck).AsOpResult()));
        if (!values) return await WriteJson(response, values);

        var (dir, lastcheck) = values.Value;
        if (!Directory.Exists(dir)) return await WriteErr(response, "Directory does not exists");

        var files = Directory.EnumerateFiles(dir, "*", SearchOption.AllDirectories)
            .Select(file => new DiffOutputFile(file, new FileInfo(file).Length, new DateTimeOffset(File.GetCreationTimeUtc(file)).ToUnixTimeMilliseconds()))
            .Where(v => v.ModifTime > lastcheck)
            .ToImmutableArray();

        return await WriteJson(response, new DiffOutput(files).AsOpResult());
    }

    public readonly record struct DiffOutput(ImmutableArray<DiffOutputFile> Files);
    public readonly record struct DiffOutputFile(string Path, long Size, long ModifTime);
}
