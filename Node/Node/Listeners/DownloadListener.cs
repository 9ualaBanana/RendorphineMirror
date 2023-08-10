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

        if (path == "taskresult")
        {
            var taskid = ReadQueryString(context.Request.QueryString, "taskid").ThrowIfError();
            var taskdir = ReceivedTask.FSPlacedResultsDirectory(taskid);

            if (!Directory.Exists(taskdir) || Directory.GetFiles(taskdir).Length == 0)
                return HttpStatusCode.NotFound;

            var zipfile = Path.GetTempFileName();
            try
            {
                ZipFile.CreateFromDirectory(taskdir, zipfile);

                using (var reader = File.OpenRead(zipfile))
                    await reader.CopyToAsync(response.OutputStream);
            }
            finally { File.Delete(zipfile); }

            return HttpStatusCode.OK;
        }
        if (path == "uploadtask")
        {
            var taskid = ReadQueryString(context.Request.QueryString, "taskid").ThrowIfError();
            var taskdir = ReceivedTask.FSPlacedResultsDirectory(taskid);

            var zipfile = Path.GetTempFileName();

            try
            {
                using (var writer = File.OpenWrite(zipfile))
                    await context.Request.InputStream.CopyToAsync(writer);

                ZipFile.ExtractToDirectory(zipfile, taskdir);
            }
            finally { File.Delete(zipfile); }

            return HttpStatusCode.OK;
        }


        var file = ReadQueryString(context.Request.QueryString, "path").ThrowIfError();
        if (!File.Exists(file)) return await WriteErr(response, "File does not exists");

        using (var reader = File.OpenRead(file))
            await reader.CopyToAsync(response.OutputStream);

        return HttpStatusCode.OK;
    }
}
