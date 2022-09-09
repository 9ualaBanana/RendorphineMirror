using System.Net;
using Transport.Upload;

namespace Node.Listeners;

public class DownloadListener : ExecutableListenerBase
{
    protected override bool IsLocal => false;
    protected override bool RequiresAuthentication => true;
    protected override string? Prefix => "download";


    protected override async Task<HttpStatusCode> ExecuteGet(string path, HttpListenerContext context)
    {
        // TODO: whitelist for files or something?

        var response = context.Response;

        var values = ReadQueryString(context.Request.QueryString, "path")
            .Next(dir => ReadQueryString(context.Request.QueryString, "url")
            .Next(url => (dir, url).AsOpResult()));
        if (!values) return await WriteJson(response, values);

        var (file, url) = values.Value;

        if (!File.Exists(file)) return await WriteErr(response, "File does not exists");

        var data = new UserUploadSessionData(url, file);
        var upload = await PacketsTransporter.UploadAsync(data);

        return await WriteSuccess(response);
    }

    readonly record struct DiffOutput(long ModifTime, ImmutableArray<string> Files);
}
