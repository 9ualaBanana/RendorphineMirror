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
        var task = NodeSettings.QueuedTasks.FirstOrDefault(x => x.Id == taskid).ThrowIfNull("No task found with such id");

        var file = sections["file"];
        using var _ = file.Body;

        var filename = Path.Combine(task.FSInputDirectory(), GetQueryPart(file.Headers["Content-Disposition"], "filename"));
        using (var resultfile = File.OpenWrite(filename))
            await file.Body.CopyToAsync(resultfile);

        if ((await (sections.GetValueOrDefault("last")?.ReadAsStringAsync() ?? Task.FromResult("0"))) == "1")
            ((DirectDownloadTaskInputInfo) task.Input).Downloaded = true;

        // TODO: ограничение на закачки

        return await WriteSuccess(context.Response);
    }
}
