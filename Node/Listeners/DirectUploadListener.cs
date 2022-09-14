using System.Net;
using Microsoft.AspNetCore.WebUtilities;

namespace Node.Listeners;

public class DirectUploadListener : MultipartListenerBase
{
    protected override ListenTypes ListenType => ListenTypes.Public;
    protected override string? Prefix => "rphtaskexec/uploadinput";

    protected override async ValueTask<HttpStatusCode> Execute(HttpListenerContext context, CachedMultipartReader reader)
    {
        var sections = await reader.GetSectionsAsync();
        var taskid = await sections["taskid"].ReadAsStringAsync();
        var task = NodeSettings.QueuedTasks.First(x => x.Id == taskid);

        var file = sections["file"];
        using var _ = file.Body;

        var filename = Path.Combine(ReceivedTask.FSInputDirectory(taskid), GetQueryPart(file.Headers["Content-Disposition"], "filename"));
        using (var resultfile = File.OpenWrite(filename))
            await file.Body.CopyToAsync(resultfile);

        ((DirectDownloadTaskInputInfo) task.Input).Downloaded = true;
        return await WriteSuccess(context.Response);
    }
}
