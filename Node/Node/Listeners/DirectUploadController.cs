using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;

namespace Node.Listeners;

[ApiController]
[Route("rphtaskexec")]
public class DirectUploadController : ControllerBase
{
    class FileList
    {
        public TaskCompletionSource Completion { get; } = new();
        public TaskFileList Files { get; }
        public string TaskId { get; }
        public TaskObject TaskObject { get; }

        public FileList(string directory, string taskid, TaskObject taskobject)
        {
            Files = new(directory);
            TaskId = taskid;
            TaskObject = taskobject;
        }
    }

    static readonly Dictionary<string, FileList> TasksToReceive = [];

    public static async Task<ReadOnlyTaskFileList> WaitForFiles(string directory, string taskid, TaskObject obj, CancellationToken token)
    {
        var filelist = new FileList(directory, taskid, obj);
        TasksToReceive.Add(taskid, filelist);

        var ttoken = new TimeoutCancellationToken(token, TimeSpan.FromHours(1));

        while (true)
        {
            if (filelist.Completion.Task.IsCompleted)
                break;

            ttoken.ThrowIfCancellationRequested();
            ttoken.ThrowIfStuck($"Did not receive input files");
            await Task.Delay(2000, token);
        }

        return filelist.Files;
    }

    [HttpPost("uploadinput")]
    public async Task<ActionResult> UploadInput([FromForm] string taskid, [FromForm] IFormFile file, [FromForm] string? last = "0")
    {
        if (!TasksToReceive.TryGetValue(taskid, out var filelist))
            return Ok(JsonApi.Error("No task found with such id"));

        var headers = file.Headers.ThrowIfNull();
        var format = FileFormatExtensions.FromMime(headers.ContentType.ToString());
        var isLast = last == "1";

        // if there is only one input file, check length
        if (filelist.Files.Count == 0 && isLast)
        {
            var length = long.Parse(headers["Content-Length"].ToString());
            if (Math.Abs(filelist.TaskObject.Size - length) > 1024 * 1024)
                return Ok(JsonApi.Error("Invalid input file length"));
        }

        var filename = filelist.Files.New(format, GetQueryPart(file.Headers.ContentDisposition, "filename"));
        if (isLast)
        {
            filelist.Completion.SetResult();
            TasksToReceive.Remove(taskid);
        }

        return PhysicalFile(filename.Path, MimeTypes.GetMimeType(filename.Path), Path.GetFileName(filename.Path));
    }

    static string GetQueryPart(StringValues values, string name) => string.Join(" ", values.WhereNotNull()).Split(name + "=")[1].Split(";")[0];
}
