using System.Net;

namespace Node.Listeners;

public class DirectoryDiffListener : ExecutableListenerBase
{
    protected override bool IsLocal => false;
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
        if (!Directory.Exists(dir)) return await WriteErr(response, "Directory does not exists  ");

        var files = Directory.EnumerateFiles(dir, "*", SearchOption.AllDirectories)
            .Select(file => (new DateTimeOffset(File.GetCreationTimeUtc(file)).ToUnixTimeMilliseconds(), Path.GetRelativePath(dir, file)))
            .Where(v => v.Item1 > lastcheck)
            .ToArray();

        var maxtime = files.Length == 0 ? 0 : files.Max(x => x.Item1);
        return await WriteJson(response, new DiffOutput(maxtime, files.Select(x => x.Item2).ToImmutableArray()).AsOpResult());
    }

    readonly record struct DiffOutput(long ModifTime, ImmutableArray<string> Files);
}
