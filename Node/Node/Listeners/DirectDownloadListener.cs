using System.IO.Compression;
using System.Net;

namespace Node.Listeners;

public class DirectDownloadListener : ExecutableListenerBase
{
    static readonly Dictionary<string, TaskCompletionSource> TasksToReceive = new();

    public required NodeDataDirs Dirs { get; init; }

    public DirectDownloadListener(ILogger<DirectDownloadListener> logger) : base(logger) { }

    protected override ListenTypes ListenType => ListenTypes.Public;
    protected override string? Prefix => "rphtaskexec/downloadoutput";

    public static async Task WaitForUpload(string taskid, CancellationToken token)
    {
        var taskcs = new TaskCompletionSource();
        lock (TasksToReceive)
            TasksToReceive[taskid] = taskcs;

        var ttoken = new TimeoutCancellationToken(token, TimeSpan.FromHours(2));


        try
        {
            while (true)
            {
                if (taskcs.Task.IsCompleted)
                    break;

                ttoken.ThrowIfCancellationRequested();
                ttoken.ThrowIfStuck($"Could not upload result");
                await Task.Delay(2000, token);
            }
        }
        finally
        {
            lock (TasksToReceive)
                TasksToReceive.Remove(taskid);
        }
    }

    protected override async Task<HttpStatusCode> ExecuteHead(string path, HttpListenerContext context)
    {
        var response = context.Response;
        var taskid = ReadQueryString(context.Request.QueryString, "taskid").ThrowIfError();

        lock (TasksToReceive)
            if (!TasksToReceive.ContainsKey(taskid))
                return HttpStatusCode.NotFound;

        // TODO: fix uploading FSOutputDirectory instead of outputfiles
        var taskdir = Dirs.TaskOutputDirectory(taskid);
        if (!Directory.Exists(taskdir))
            return HttpStatusCode.NotFound;

        var dirfiles = Directory.GetFiles(taskdir, "*", SearchOption.AllDirectories);
        if (dirfiles.Length == 0)
            return HttpStatusCode.NotFound;

        if (dirfiles.Length == 1)
        {
            var file = dirfiles[0];

            response.Headers.Add("Content-Disposition", $"form-data; name=file; filename={Path.GetFileName(file)}");
            response.ContentLength64 = new FileInfo(file).Length;
            response.ContentType = MimeTypes.GetMimeType(file);

            return HttpStatusCode.OK;
        }

        return await base.ExecuteHead(path, context);
    }
    protected override async Task<HttpStatusCode> ExecuteGet(string path, HttpListenerContext context)
    {
        var response = context.Response;
        var taskid = ReadQueryString(context.Request.QueryString, "taskid").ThrowIfError();

        if (!TasksToReceive.TryGetValue(taskid, out var taskcs))
            return await WriteErr(response, "No task found with such id");

        // TODO: fix uploading FSOutputDirectory instead of outputfiles
        var taskdir = Dirs.TaskOutputDirectory(taskid);
        if (!Directory.Exists(taskdir)) return HttpStatusCode.NotFound;

        var dirfiles = Directory.GetFiles(taskdir, "*", SearchOption.AllDirectories);
        if (dirfiles.Length == 0)
            throw new Exception($"No task result was found in the directory {taskdir}");

        if (dirfiles.Length == 1)
        {
            var file = dirfiles[0];

            response.Headers.Add("Content-Disposition", $"form-data; name=file; filename={Path.GetFileName(file)}");
            response.ContentLength64 = new FileInfo(file).Length;
            response.ContentType = MimeTypes.GetMimeType(file);

            using (var reader = File.OpenRead(file))
                await reader.CopyToAsync(response.OutputStream);

            taskcs.SetResult();
            return HttpStatusCode.OK;
        }

        var zipfile = Path.GetTempFileName();
        try
        {
            File.Delete(zipfile);
            ZipFile.CreateFromDirectory(taskdir, zipfile);

            response.Headers.Add("Content-Disposition", "form-data; name=\"file\"; filename=\"file.zip\"");
            response.ContentLength64 = new FileInfo(zipfile).Length;
            response.ContentType = "application/zip";

            using (var reader = File.OpenRead(zipfile))
                await reader.CopyToAsync(response.OutputStream);

            taskcs.SetResult();
        }
        catch { return HttpStatusCode.InternalServerError; }
        finally { File.Delete(zipfile); }

        return HttpStatusCode.OK;
    }
}
