using System.IO.Compression;
using Microsoft.AspNetCore.Mvc;

namespace Node.Listeners;

[ApiController]
[Route("rphtaskexec")]
public class DirectDownloadController : ControllerBase
{
    static readonly Dictionary<string, TaskCompletionSource> TasksToReceive = [];

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

    [HttpHead("downloadoutput")]
    public async Task<ActionResult> GetOutputHead([FromQuery] string taskid, [FromServices] NodeDataDirs dirs)
    {
        if (!TasksToReceive.TryGetValue(taskid, out var taskcs))
            return NotFound();

        // TODO: fix uploading FSOutputDirectory instead of outputfiles
        var taskdir = dirs.TaskOutputDirectory(taskid);
        if (!Directory.Exists(taskdir))
            return NotFound();

        var dirfiles = Directory.GetFiles(taskdir, "*", SearchOption.AllDirectories);
        if (dirfiles.Length == 0)
            throw new Exception($"No task result was found in the directory {taskdir}");

        if (dirfiles.Length == 1)
        {
            var file = dirfiles[0];

            Response.Headers.ContentDisposition = $"form-data; name=file; filename={Path.GetFileName(file)}";
            Response.ContentLength = new FileInfo(file).Length;
            Response.ContentType = MimeTypes.GetMimeType(file);

            return Ok();
        }

        return NotFound();
    }

    [HttpGet("downloadoutput")]
    public async Task<ActionResult> DownloadOutput([FromQuery] string taskid, [FromServices] NodeDataDirs dirs)
    {
        if (!TasksToReceive.TryGetValue(taskid, out var taskcs))
            return Ok(JsonApi.Error("No task found with such id"));

        // TODO: fix uploading FSOutputDirectory instead of outputfiles
        var taskdir = dirs.TaskOutputDirectory(taskid);
        if (!Directory.Exists(taskdir))
            return NotFound();

        var dirfiles = Directory.GetFiles(taskdir, "*", SearchOption.AllDirectories);
        if (dirfiles.Length == 0)
            throw new Exception($"No task result was found in the directory {taskdir}");

        if (dirfiles.Length == 1)
        {
            var file = dirfiles[0];

            taskcs.SetResult();
            return PhysicalFile(file, MimeTypes.GetMimeType(file), Path.GetFileName(file), false);
        }

        var zipfile = Path.GetTempFileName();
        try
        {
            System.IO.File.Delete(zipfile);
            ZipFile.CreateFromDirectory(taskdir, zipfile);
            var file = zipfile;

            taskcs.SetResult();
            return new DeletedPhysicalFileResult(file, "application/zip")
            {
                FileName = "file.zip",
                EnableRangeProcessing = false
            };
        }
        finally
        {
            if (System.IO.File.Exists(zipfile))
                System.IO.File.Delete(zipfile);
        }
    }


    class DeletedPhysicalFileResult : PhysicalFileResult
    {
        public DeletedPhysicalFileResult(string fileName, string contentType) : base(fileName, contentType) { }

        public override async Task ExecuteResultAsync(ActionContext context)
        {
            try { await base.ExecuteResultAsync(context); }
            finally { System.IO.File.Delete(FileName); }
        }
    }
}
