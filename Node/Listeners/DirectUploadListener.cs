using System.Net;
using Microsoft.AspNetCore.WebUtilities;
using Node.Tasks;

namespace Node.Listeners;

public class DirectUploadListener : MultipartListenerBase
{
    protected override ListenTypes ListenType => ListenTypes.Public;
    protected override string? Prefix => "rphtaskexec/uploadinput";

    protected override async ValueTask<HttpStatusCode> Execute(HttpListenerContext context, CachedMultipartReader reader)
    {
        var sections = await reader.GetSectionsAsync();
        var taskid = await sections["taskid"].ReadAsStringAsync();
        var task = NodeSettings.QueuedTasks.FirstOrDefault(x => x.Id == taskid);

        if (task is null || task.Input is not DirectDownloadTaskInputInfo dinput)
            return await WriteErr(context.Response, "No task found with such id");

        if (dinput.Downloaded)
            return await WriteErr(context.Response, "Input files were already uploaded");

        var last = (await (sections.GetValueOrDefault("last")?.ReadAsStringAsync() ?? Task.FromResult("0"))) == "1";
        if (last)
        {
            var check = task.GetAction().InputRequirements.Check(task);
            if (!check) return await WriteJson(context.Response, check);
        }

        var file = sections["file"];
        var format = FileFormatExtensions.FromMime(file.Headers["Content-Type"]);

        using var _ = file.Body;

        var filename = task.FSNewInputFile(format, GetQueryPart(file.Headers["Content-Disposition"], "filename"));
        using (var resultfile = File.OpenWrite(filename))
            await file.Body.CopyToAsync(resultfile);

        if (last) dinput.Downloaded = true;
        return await WriteSuccess(context.Response);
    }
}
