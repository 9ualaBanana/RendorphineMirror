using System.IO.Compression;
using System.Net;

namespace Node.Listeners;

public class DownloadListener : ExecutableListenerBase
{
    protected override ListenTypes ListenType => ListenTypes.Public;
    protected override bool RequiresAuthentication => true;
    protected override string? Prefix => "download";

    public DownloadListener(ILogger<DownloadListener> logger) : base(logger) { }


    protected override async Task<HttpStatusCode> ExecuteGet(string path, HttpListenerContext context)
    {
        // TODO: whitelist for files or something?
        // TODO: switch to torrent

        var response = context.Response;

        var file = ReadQueryString(context.Request.QueryString, "path").ThrowIfError();
        if (!File.Exists(file)) return await WriteErr(response, "File does not exists");

        using (var reader = File.OpenRead(file))
            await reader.CopyToAsync(response.OutputStream);

        return HttpStatusCode.OK;
    }
}
