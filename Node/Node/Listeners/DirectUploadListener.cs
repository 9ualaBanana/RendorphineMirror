using Microsoft.AspNetCore.WebUtilities;
using System.Net;

namespace Node.Listeners;

public class DirectUploadListener : MultipartListenerBase
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

    static readonly Dictionary<string, FileList> TasksToReceive = new();

    protected override ListenTypes ListenType => ListenTypes.Public;
    protected override string? Prefix => "rphtaskexec/uploadinput";

    public DirectUploadListener(ILogger<DirectUploadListener> logger) : base(logger) { }

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

    protected override async ValueTask<HttpStatusCode> Execute(HttpListenerContext context, CachedMultipartReader reader)
    {
        var sections = await reader.GetSectionsAsync();
        var taskid = await sections["taskid"].ReadAsStringAsync();

        if (!TasksToReceive.TryGetValue(taskid, out var filelist))
            return await WriteErr(context.Response, "No task found with such id");


        var file = sections["file"];
        var format = FileFormatExtensions.FromMime(file.Headers["Content-Type"]);
        var last = (await (sections.GetValueOrDefault("last")?.ReadAsStringAsync() ?? Task.FromResult("0"))) == "1";

        // if there is only one input file, check length
        if (filelist.Files.Count == 0 && last)
        {
            var length = long.Parse(file.Headers["Content-Length"]);
            if (Math.Abs(filelist.TaskObject.Size - length) > 1024 * 1024)
                return await WriteErr(context.Response, "Invalid input file length");
        }

        using var _ = file.Body;

        var filename = filelist.Files.New(format, GetQueryPart(file.Headers["Content-Disposition"], "filename"));
        using (var resultfile = File.OpenWrite(filename.Path))
            await file.Body.CopyToAsync(resultfile);


        if (last)
        {
            filelist.Completion.SetResult();
            TasksToReceive.Remove(taskid);
        }

        return await WriteSuccess(context.Response);
    }
}
